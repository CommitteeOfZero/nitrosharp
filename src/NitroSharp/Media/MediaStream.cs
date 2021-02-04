using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.VisualStudio.Threading;
using NitroSharp.Media.XAudio2;
using NitroSharp.Utilities;
using Veldrid;
using static NitroSharp.Media.FFmpegUtil;

// The AV sync code is based on ffplay's implementation.
// ffplay is part of the FFmpeg project.

namespace NitroSharp.Media
{
    internal sealed class LoopRegion
    {
        public TimeSpan Start { get; }
        public TimeSpan End { get; }

        public LoopRegion(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }
    }

    internal sealed class MediaStream : IDisposable
    {
        private const int IoBufferSize = 4096;
        private const int ErrorEAgain = -11;

        private const double SyncThresholdMin = 0.04;
        private const double SyncThresholdMax = 0.1;
        private const double FrameDupThreshold = 0.1;
        private const double AvNosyncThreshold = 10.0;

        // Lock for avcodec_open2. Not 100% sure it's necessary.
        private static readonly object s_lock = new();

        private readonly Stream _fileStream;
        private readonly unsafe AVFormatContext* _formatContext;
        private readonly StreamContext? _audio;
        private readonly StreamContext? _video;
        private readonly Pipe? _audioPipe;
        private readonly XAudio2AudioSource? _audioSource;
        private readonly AudioParameters _outAudioParams;
        private readonly YCbCrBuffer? _videoBuffer;
        private readonly Size? _videoResolution;
        private readonly double _videoFrameDuration;
        private readonly double _maxFrameDuration;

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        // The delegates must be kept alive for the entire lifetime of the object.
        private readonly avio_alloc_context_read_packet _readFunc;
        private readonly avio_alloc_context_seek _seekFunc;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        private readonly List<Task> _tasks = new();
        private Task? _combinedTask;
        private readonly CancellationTokenSource _cts = new();

        private double _startTimestamp;
        private bool _loopingEnabled;
        private LoopRegion? _loopRegion;
        private SeekRequest? _seekRequest;
        private bool _started;
        private bool _ended;

        private readonly AsyncManualResetEvent _unpauseSignal = new(initialState: false);

        private VideoFrameInfo _lastDisplayedFrameInfo;
        private double _frameTimer;
        private readonly Stopwatch _timer = new();

        private Clock _videoClock;
        private Clock _externalClock;

        public bool IsPlaying
            => _combinedTask is { IsCompleted: false } && !_ended || (_audioSource?.IsPlaying ?? false);

        private struct Clock
        {
            private readonly StreamContext? _stream;
            private readonly Stopwatch _timer;
            private readonly AsyncManualResetEvent _unpauseSignal;
            private double _timestamp;
            private double _timestampDrift;
            private int _serial;

            public Clock(
                StreamContext? stream,
                Stopwatch timer,
                AsyncManualResetEvent unpauseSignal)
            {
                _stream = stream;
                _timer = timer;
                _unpauseSignal = unpauseSignal;
                _timestamp = double.NaN;
                _serial = -1;
                _timestampDrift = 0;
                LastUpdated = 0;
            }

            public double LastUpdated { get; private set; }

            public readonly double Get()
            {
                if ((_stream?.Serial ?? _serial) != _serial) { return double.NaN; }
                if (!_unpauseSignal.IsSet) { return _timestamp; }
                return _timestampDrift + _timer.Elapsed.TotalSeconds;
            }

            public void Set(double timestamp, int serial)
            {
                _timestamp = timestamp;
                double time = _timer.Elapsed.TotalSeconds;
                _timestampDrift = _timestamp - time;
                _serial = serial;
                LastUpdated = time;
            }

            public void SyncTo(in Clock other)
            {
                double thisValue = Get();
                double otherValue = other.Get();
                if (!double.IsNaN(otherValue) && (double.IsNaN(thisValue)
                    || Math.Abs(thisValue - otherValue) > AvNosyncThreshold))
                {
                    Set(otherValue, other._serial);
                }
            }

            public void Refresh() => Set(Get(), _serial);
        }

        private sealed class SeekRequest
        {
            public SeekRequest(TimeSpan target, bool flushAudioSource)
            {
                Target = target;
                //FlushAudioSource = flushAudioSource;
            }

            public TimeSpan Target { get; }
            //public bool FlushAudioSource { get; }
        }

        private struct QueueItem<T> where T : struct
        {
            public T Value;
            public int Serial;

            public bool Flush => Serial == int.MaxValue;
            public bool Eof => Serial == int.MinValue;
        }

        private sealed class StreamContext : IDisposable
        {
            private readonly unsafe AVStream* _stream;

            public readonly int Index;
            public unsafe AVCodecContext* CodecCtx;

            public readonly Channel<QueueItem<AVPacket>> PacketQueue;
            public readonly Channel<QueueItem<AVFrame>> FrameQueue;

            public int Serial;
            public SeekRequest? SeekRequest;

            public unsafe StreamContext(
                AVStream* stream,
                AVCodecContext* codecCtx,
                int packetQueueSize,
                int frameQueueSize)
            {
                _stream = stream;
                Index = stream->index;
                CodecCtx = codecCtx;

                var packetQueueOptions = new BoundedChannelOptions(packetQueueSize)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                };
                var frameQueueOptions = new BoundedChannelOptions(frameQueueSize)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                };

                PacketQueue = Channel.CreateBounded<QueueItem<AVPacket>>(packetQueueOptions);
                FrameQueue = Channel.CreateBounded<QueueItem<AVFrame>>(frameQueueOptions);
            }

            public unsafe AVRational TimeBase => _stream->time_base;

            public void DestroyQueues()
            {
                while (FrameQueue.Reader.TryRead(out QueueItem<AVFrame> frame))
                {
                    UnrefFrame(ref frame.Value);
                }
                while (PacketQueue.Reader.TryRead(out QueueItem<AVPacket> packet))
                {
                    UnrefPacket(ref packet.Value);
                }
                FrameQueue.Writer.Complete();
                PacketQueue.Writer.Complete();
            }

            public unsafe void Dispose()
            {
                fixed (AVCodecContext** ctx = &CodecCtx)
                {
                    ffmpeg.avcodec_free_context(ctx);
                }
            }
        }

        public unsafe MediaStream(
            Stream stream,
            GraphicsDevice? graphicsDevice,
            XAudio2AudioSource? audioSource,
            AudioParameters outAudioParams)
        {
            _fileStream = stream;
            _readFunc = IoReadPacket;
            _seekFunc = IoSeek;
            _audioSource = audioSource;
            _outAudioParams = outAudioParams;

            // Both the buffer and the IO context are freed by avformat_close_input.
            byte* ioBuffer = (byte*)ffmpeg.av_malloc(IoBufferSize);
            AVIOContext* ioContext = ffmpeg.avio_alloc_context(
                ioBuffer, IoBufferSize,
                write_flag: 0, opaque: null,
                _readFunc, null, _seekFunc
            );

            AVFormatContext* ctx = ffmpeg.avformat_alloc_context();
            ctx->pb = ioContext;
            _formatContext = ctx;

            CheckResult(ffmpeg.avformat_open_input(&ctx, string.Empty, null, null));
            CheckResult(ffmpeg.avformat_find_stream_info(ctx, null));

            int audioStreamId = -1;
            int videoStreamId = -1;
            double duration = 0;

            if (audioSource is not null)
            {
                audioStreamId = ffmpeg.av_find_best_stream(
                    ctx,
                    AVMediaType.AVMEDIA_TYPE_AUDIO,
                    -1, -1,
                    null, 0
                );
                if (audioStreamId >= 0)
                {
                    AVStream* s = ctx->streams[audioStreamId];
                    duration = Math.Max(duration, s->duration * s->time_base.ToDouble());
                }
            }
            if (graphicsDevice is not null)
            {
                videoStreamId = ffmpeg.av_find_best_stream(
                    ctx,
                    AVMediaType.AVMEDIA_TYPE_VIDEO,
                    -1, -1,
                    null, 0
                );
                if (videoStreamId >= 0)
                {
                    AVStream* s = ctx->streams[videoStreamId];
                    duration = Math.Max(duration, s->duration * s->time_base.ToDouble());
                    _videoResolution = new Size(
                        (uint)s->codecpar->width,
                        (uint)s->codecpar->height
                    );
                    AVRational framerate = ffmpeg.av_guess_frame_rate(ctx, s, null);
                    _videoFrameDuration = framerate.den > 0
                        ? new AVRational { num = framerate.den, den = framerate.num }.ToDouble()
                        : 0;
                    _maxFrameDuration =
                        (ctx->iformat->flags & ffmpeg.AVFMT_TS_DISCONT) == ffmpeg.AVFMT_TS_DISCONT
                            ? 10.0d
                            : 3600.0d;
                }
            }

            Duration = TimeSpan.FromSeconds(duration);

            for (int i = 0; i < ctx->nb_streams; i++)
            {
                bool active = i == videoStreamId || i == audioStreamId;
                ctx->streams[i]->discard = active
                    ? AVDiscard.AVDISCARD_DEFAULT
                    : AVDiscard.AVDISCARD_ALL;
            }

            if (audioSource is { } && (_audio = OpenStream(_formatContext, audioStreamId)) is { })
            {
                var options = new PipeOptions(minimumSegmentSize: 16384);
                _audioPipe = new Pipe(options);
            }
            if (graphicsDevice is { } && (_video = OpenStream(ctx, videoStreamId)) is { })
            {
                _videoBuffer = new YCbCrBuffer(
                    graphicsDevice,
                    (uint)_video.CodecCtx->width,
                    (uint)_video.CodecCtx->height
                );
            }
        }

        public TimeSpan Duration { get; }

        public XAudio2AudioSource AudioSource => _audioSource!;

        public Size VideoResolution
        {
            get
            {
                if (_videoResolution is null)
                {
                    NoVideoStream();
                    return default;
                }

                return _videoResolution.Value;
            }
        }

        public YCbCrBufferReader VideoFrames
        {
            get
            {
                if (_videoBuffer is not YCbCrBuffer buffer)
                {
                    NoVideoStream();
                    return default;
                }

                return buffer.Reader;
            }
        }

        private static unsafe StreamContext? OpenStream(AVFormatContext* formatCtx, int index)
        {
            if (index < 0) { return null; }
            AVStream* stream = formatCtx->streams[index];
            AVCodec* codec = DecoderCollection.Shared.Get(stream->codecpar->codec_id);
            AVCodecContext* codecCtx = ffmpeg.avcodec_alloc_context3(codec);
            Debug.Assert(codecCtx is not null);
            CheckResult(ffmpeg.avcodec_parameters_to_context(codecCtx, stream->codecpar));
            lock (s_lock)
            {
                CheckResult(ffmpeg.avcodec_open2(codecCtx, codec, null));
            }

            if (codecCtx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                return new StreamContext(stream, codecCtx, 3000, 3000);
            }

            return new StreamContext(stream, codecCtx, 256, 48);
        }

        public void Start()
        {
            if (_started) { return; }
            _started = true;
            _unpauseSignal.Set();
            _tasks.Add(Task.Run(Read));
            if (_audio is { } && _audioSource is XAudio2AudioSource audioSource)
            {
                Debug.Assert(_audioPipe is not null);
                _tasks.Add(Task.Run(() => Decode(_audio)));
                _tasks.Add(Task.Run(() => ProcessAudio(
                    _audio,
                    _outAudioParams,
                    _audioPipe.Writer
                )));
                audioSource.Play(_audioPipe.Reader);
            }

            if (_video is { } && _videoBuffer is { })
            {
                _tasks.Add(Task.Run(() => Decode(_video)));
                _tasks.Add(Task.Run(() => ProcessVideo(_video, _videoBuffer.Writer)));
            }

            _combinedTask = Task.WhenAll(_tasks);
            _videoClock = new Clock(_video, _timer, _unpauseSignal);
            _externalClock = new Clock(null, _timer, _unpauseSignal);
            _timer.Start();
        }

        public void ToggleLooping(bool enable)
            => _loopingEnabled = enable;

        public void SetLoopRegion(LoopRegion loopRegion)
            => _loopRegion = loopRegion;

        public void Pause()
        {
            if (_unpauseSignal.IsSet)
            {
                _externalClock.Refresh();
                _audioSource?.Pause();
                _unpauseSignal.Reset();
            }
        }

        public void Resume()
        {
            if (!_unpauseSignal.IsSet)
            {
                _frameTimer += _timer.Elapsed.TotalSeconds - _videoClock.LastUpdated;
                _videoClock.Refresh();
                _externalClock.Refresh();
                _audioSource?.Resume();
                _unpauseSignal.Set();
            }
        }

        public bool GetNextFrame(out YCbCrFrame frame)
        {
            double frameDuration(in VideoFrameInfo frame, in VideoFrameInfo nextFrame)
            {
                if (frame.Serial == nextFrame.Serial)
                {
                    double duration = nextFrame.Timestamp - frame.Timestamp;
                    return double.IsNaN(duration) || duration <= 0.0d || duration > _maxFrameDuration
                        ? frame.Duration
                        : duration;
                }

                return 0.0d;
            }

            Debug.Assert(_video is not null);
            YCbCrBufferReader frames = VideoFrames;
            while (frames.PeekFrame(out frame))
            {
                if (frame.Serial != _video.Serial)
                {
                    frame.Dispose();
                    continue;
                }

                if (FrameIsClosestTo(frame.GetInfo(), Duration))
                {
                    _ended = true;
                }

                if (!_unpauseSignal.IsSet) { return false; }

                double time = _timer.Elapsed.TotalSeconds;
                if (_frameTimer == 0 || _lastDisplayedFrameInfo.Serial != frame.Serial)
                {
                    _frameTimer = time;
                }

                double duration = frameDuration(_lastDisplayedFrameInfo, frame.GetInfo());
                double targetDelay = CalcTargetDelay(duration);

                if (_lastDisplayedFrameInfo.Duration > 0 && time < _frameTimer + targetDelay)
                {
                    break;
                }

                _frameTimer += targetDelay;
                if (targetDelay > 0 && time - _frameTimer > SyncThresholdMax)
                {
                    _frameTimer = time;
                }

                _videoClock.Set(frame.Timestamp, frame.Serial);
                _externalClock.SyncTo(_videoClock);

                _lastDisplayedFrameInfo = frame.GetInfo();

                if (time <= _frameTimer + duration)
                {
                    if (_audio is null && _loopingEnabled && _loopRegion is LoopRegion loopRegion)
                    {
                        if (FrameIsClosestTo(_lastDisplayedFrameInfo, loopRegion.End))
                        {
                            Seek(loopRegion.Start, flushAudioSource: false);
                        }
                    }

                    return true;
                }

                frame.Dispose();
            }

            return false;
        }

        private double CalcTargetDelay(double delay)
        {
            double diff = _videoClock.Get() - GetPlaybackPosition();
            double syncThreshold = MathUtil.Clamp(delay, SyncThresholdMin, SyncThresholdMax);
            if (!double.IsNaN(diff) && Math.Abs(diff) < _maxFrameDuration)
            {
                if (diff <= -syncThreshold)
                {
                    delay = Math.Max(0, delay + diff);
                }
                else if (diff >= syncThreshold)
                {
                    if (delay <= FrameDupThreshold)
                    {
                        delay *= 2;
                    }
                    else
                    {
                        delay += diff;
                    }
                }
            }

            return delay;
        }

        private double GetPlaybackPosition()
        {
            //if (_audioSource is not null)
            //{
            //    return _startTimestamp + _audioSource.GetPlaybackPosition();
            //}

            Debug.Assert(_videoBuffer is not null);
            return _externalClock.Get();
        }

        private void Seek(TimeSpan target, bool flushAudioSource = true)
        {
            _seekRequest = new SeekRequest(target, flushAudioSource);
        }

        private async Task Read()
        {
            QueueItem<AVPacket> packet = default;
            int serial = 0;
            bool ignoreEof = false;
            while (!_cts.IsCancellationRequested)
            {
                await _unpauseSignal.WaitAsync();
                if (_seekRequest is SeekRequest seekRequest)
                {
                    _seekRequest = null;
                    TimeSpan seekTarget = seekRequest.Target;
                    long timestamp = (long)Math.Round(seekTarget.TotalSeconds * ffmpeg.AV_TIME_BASE);
                    unsafe
                    {
                        CheckResult(ffmpeg.avformat_seek_file(
                            _formatContext, -1,
                            timestamp - 0 * ffmpeg.AV_TIME_BASE, timestamp, timestamp,
                            flags: 0
                        ));
                    }

                    serial++;
                    _startTimestamp = seekTarget.TotalSeconds;
                    _externalClock.Set(seekTarget.TotalSeconds, serial: 0);

                    if (_audio is not null)
                    {
                        Interlocked.Increment(ref _audio.Serial);
                        _audio.SeekRequest = seekRequest;
                        //if (seekRequest.FlushAudioSource)
                        //{
                        //    Debug.Assert(_audioSource is not null);
                        //    _audioSource.Pause();
                        //    _audioSource.FlushBuffers();
                        //    _audioSource.Resume();
                        //}
                    }
                    if (_video is not null)
                    {
                        Interlocked.Increment(ref _video.Serial);
                        _video.SeekRequest = seekRequest;
                    }

                    packet.Value = default;
                    packet.Serial = int.MaxValue;
                    if (_audio is not null)
                    {
                        await _audio.PacketQueue.Writer.WriteAsync(packet);
                    }
                    if (_video is not null)
                    {
                        await _video.PacketQueue.Writer.WriteAsync(packet);
                    }
                }

                int ret;
                unsafe
                {
                    ret = ffmpeg.av_read_frame(_formatContext, &packet.Value);
                }
                if (ret >= 0)
                {
                    ignoreEof = false;
                    StreamContext streamCtx;
                    if (packet.Value.stream_index == _video?.Index)
                    {
                        streamCtx = _video;
                    }
                    else if (packet.Value.stream_index == _audio?.Index)
                    {
                        streamCtx = _audio;
                    }
                    else
                    {
                        UnrefPacket(ref packet.Value);
                        continue;
                    }

                    packet.Serial = serial;
                    await streamCtx.PacketQueue.Writer.WriteAsync(packet);
                }
                else if (ret == ffmpeg.AVERROR_EOF && !ignoreEof)
                {
                    ignoreEof = true;
                    packet.Value = default;
                    packet.Serial = serial;
                    if (_audio is StreamContext audio)
                    {
                        await audio.PacketQueue.Writer.WriteAsync(packet);
                    }
                    if (_video is StreamContext video)
                    {
                        await video.PacketQueue.Writer.WriteAsync(packet);
                    }
                }
                else
                {
                    packet.Value = default;
                    packet.Serial = int.MinValue;
                    if (_audio is StreamContext audio)
                    {
                        await audio.PacketQueue.Writer.WriteAsync(packet);
                    }
                    if (_video is StreamContext video)
                    {
                        await video.PacketQueue.Writer.WriteAsync(packet);
                    }

                    if (!_loopingEnabled) { break; }
                    var wait = new SpinWait();
                    while (!_cts.IsCancellationRequested && _seekRequest is null)
                    {
                        wait.SpinOnce();
                    }
                }
            }
        }

        private async Task Decode(StreamContext ctx)
        {
            ChannelReader<QueueItem<AVPacket>> reader = ctx.PacketQueue.Reader;
            ChannelWriter<QueueItem<AVFrame>> writer = ctx.FrameQueue.Writer;
            while (!_cts.IsCancellationRequested)
            {
                await _unpauseSignal.WaitAsync();
                QueueItem<AVPacket> packet;
                bool packetSent;
                try
                {
                    packet = await reader.ReadAsync();
                    packetSent = false;
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                if (packet.Serial != ctx.Serial)
                {
                    if (packet.Flush)
                    {
                        unsafe
                        {
                            ffmpeg.avcodec_flush_buffers(ctx.CodecCtx);
                        }
                    }
                    else if (packet.Eof)
                    {
                        QueueItem<AVFrame> eofFrame = default;
                        eofFrame.Serial = int.MinValue;
                        await writer.WriteAsync(eofFrame);
                        if (!_loopingEnabled) { break; }
                    }
                    UnrefPacket(ref packet.Value);
                    continue;
                }

            receive_frames:
                int ret;
                do
                {
                    QueueItem<AVFrame> frame = default;
                    frame.Serial = packet.Serial;
                    unsafe
                    {
                        ret = ffmpeg.avcodec_receive_frame(ctx.CodecCtx, &frame.Value);
                        if (frame.Value.extended_data == &frame.Value.data)
                        {
                            // if extended_data points to data, the pointer will become
                            // invalid once the struct is moved in memory.
                            // It should be ok to set it to null in that case.
                            frame.Value.extended_data = null;
                        }
                    }

                    if (ret == ErrorEAgain) { break; }
                    if (ret == ffmpeg.AVERROR_EOF)
                    {
                        unsafe
                        {
                            ffmpeg.avcodec_flush_buffers(ctx.CodecCtx);
                        }

                        ret = 0;
                        continue;
                    }

                    CheckResult(ret);

                    if (ctx.SeekRequest is SeekRequest seekRequest)
                    {
                        if (FrameIsClosestTo(ctx, frame.Value, seekRequest.Target))
                        {
                            ctx.SeekRequest = null;
                        }
                        else
                        {
                            UnrefFrame(ref frame.Value);
                            continue;
                        }
                    }

                    try
                    {
                        await writer.WriteAsync(frame);
                    }
                    catch (ChannelClosedException)
                    {
                        UnrefFrame(ref frame.Value);
                        UnrefPacket(ref packet.Value);
                        return;
                    }
                } while (ret >= 0);

                if (!packetSent)
                {
                    packetSent = true;
                    unsafe
                    {
                        if (!packet.Value.IsNullPacket())
                        {
                            CheckResult(ffmpeg.avcodec_send_packet(ctx.CodecCtx, &packet.Value));
                        }
                        else
                        {
                            CheckResult(ffmpeg.avcodec_send_packet(ctx.CodecCtx, null));
                            goto receive_frames;
                        }
                    }

                    UnrefPacket(ref packet.Value);
                }
            }
        }

        private async Task ProcessAudio(
            StreamContext audio,
            AudioParameters desiredParams,
            PipeWriter writer)
        {
            Resampler resampler;
            unsafe
            {
                AVCodecContext* codec = audio.CodecCtx;
                resampler = new Resampler(codec, desiredParams);
            }

            ChannelReader<QueueItem<AVFrame>> audioFrames = audio.FrameQueue.Reader;
            int bytesBuffered = 0;
            while (!_cts.IsCancellationRequested)
            {
                await _unpauseSignal.WaitAsync();
                QueueItem<AVFrame> frame;
                try
                {
                    frame = await audioFrames.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                if (frame.Serial != audio.Serial)
                {
                    if (frame.Eof)
                    {
                        bytesBuffered = 0;
                        await writer.FlushAsync();
                        if (_loopingEnabled && _loopRegion is null)
                        {
                            Seek(TimeSpan.Zero, flushAudioSource: false);
                        }
                        else if (!_loopingEnabled)
                        {
                            break;
                        }
                    }
                    UnrefFrame(ref frame.Value);
                    continue;
                }

                double timestamp = frame.Value.best_effort_timestamp * audio.TimeBase.ToDouble();
                BufferRequirements bufferReqs = resampler.GetBufferRequirements(frame.Value);
                Memory<byte> buffer = writer.GetMemory(bufferReqs.SizeInBytes);
                int bytesWritten = resampler.Convert(frame.Value, bufferReqs, buffer.Span);

                bool forceFlush = false;
                if (_loopingEnabled && _loopRegion is LoopRegion loopRegion)
                {
                    double loopEnd = loopRegion.End.TotalSeconds;
                    if (FrameIsClosestTo(audio, frame.Value, loopRegion.End))
                    {
                        if (loopEnd > timestamp)
                        {
                            var actualDuration = TimeSpan.FromSeconds(loopEnd - timestamp);
                            bytesWritten = resampler.GetBufferSize(actualDuration);
                            forceFlush = true;
                        }
                        Seek(loopRegion.Start, flushAudioSource: false);
                    }
                }

                UnrefFrame(ref frame.Value);
                writer.Advance(bytesWritten);
                bytesBuffered += bytesWritten;
                if (bytesBuffered >= 16384 || forceFlush)
                {
                    bytesBuffered = 0;
                    await writer.FlushAsync();
                }
            }

            await writer.CompleteAsync();
            resampler.Dispose();
        }

        private async Task ProcessVideo(StreamContext video, YCbCrBufferWriter bufferWriter)
        {
            ChannelReader<QueueItem<AVFrame>> videoFrames = video.FrameQueue.Reader;
            while (!_cts.IsCancellationRequested)
            {
                await _unpauseSignal.WaitAsync();
                QueueItem<AVFrame> frame;
                try
                {
                    frame = await videoFrames.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    break;
                }
                if (frame.Serial != video.Serial)
                {
                    if (frame.Flush)
                    {
                        bufferWriter.Clear();
                    }
                    else if (frame.Eof)
                    {
                        if (_loopingEnabled && _loopRegion is null)
                        {
                            Seek(TimeSpan.Zero, flushAudioSource: false);
                        }
                        else if (!_loopingEnabled)
                        {
                            break;
                        }
                    }
                    UnrefFrame(ref frame.Value);
                    continue;
                }

                double timestamp = video.TimeBase.ToDouble() * frame.Value.best_effort_timestamp;
                await bufferWriter.WriteFrameAsync(frame.Value, frame.Serial, timestamp, _videoFrameDuration);
                UnrefFrame(ref frame.Value);
            }
        }

        private bool FrameIsClosestTo(StreamContext ctx, in AVFrame frame, TimeSpan timestamp)
        {
            double pts = frame.best_effort_timestamp * ctx.TimeBase.ToDouble();
            double duration = frame.width > 0
                ? _videoFrameDuration
                : (double)frame.nb_samples / frame.sample_rate;
            double ts = timestamp.TotalSeconds;
            return Math.Abs(ts - pts) < duration;
        }

        private bool FrameIsClosestTo(in VideoFrameInfo frame, TimeSpan timestamp)
        {
            return Math.Abs(timestamp.TotalSeconds - frame.Timestamp) < _videoFrameDuration;
        }

        private static unsafe void UnrefPacket(ref AVPacket packet)
        {
            fixed (AVPacket* pPacket = &packet)
            {
                ffmpeg.av_packet_unref(pPacket);
            }
        }

        private static unsafe void UnrefFrame(ref AVFrame frame)
        {
            fixed (AVFrame* pFrame = &frame)
            {
                // extended_data normally points to the data field, which means
                // the pointer must be updated whenever the struct is moved in memory.
                //pFrame->extended_data = (byte**)&pFrame->data;
                ffmpeg.av_frame_unref(pFrame);
            }
        }

        private unsafe long IoSeek(void* opaque, long offset, int whence)
        {
            Debug.Assert(_fileStream.CanSeek);
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return _fileStream.Length;
            }

            var origin = (SeekOrigin)whence;
            return _fileStream.Seek(offset, origin);
        }

        private unsafe int IoReadPacket(void* opaque, byte* buf, int bufSize)
        {
            Debug.Assert(_fileStream.CanRead);
            int size = Math.Min(bufSize, IoBufferSize);
            var dst = new Span<byte>(buf, size);
            int result = _fileStream.Read(dst);
            return result == 0 ? ffmpeg.AVERROR_EOF : result;
        }

        public unsafe void Dispose()
        {
            _audioSource?.Pause();
            _audioSource?.FlushBuffers();
            _cts.Cancel();
            _videoBuffer?.Writer.Clear();
            _video?.DestroyQueues();
            _audio?.DestroyQueues();

            Task.WhenAll(_tasks).Wait();
            _video?.Dispose();
            _audio?.Dispose();
            _videoBuffer?.Dispose();

            if (_audioSource is not null)
            {
                Debug.Assert(_audioSource is not null);
                _audioSource.Stop();
            }

            fixed (AVFormatContext** pCtx = &_formatContext)
            {
                ffmpeg.avformat_close_input(pCtx);
            }

            _fileStream.Dispose();
            _timer.Stop();
        }

        private static void NoVideoStream()
          => throw new InvalidOperationException("Media file does not contain a video stream.");
    }
}
