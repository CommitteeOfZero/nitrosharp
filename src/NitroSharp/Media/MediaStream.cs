using System;
using System.Buffers;
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

        private readonly Stream _fileStream;
        private readonly unsafe AVFormatContext* _formatContext;
        private readonly unsafe AVPacket* _recvPacket;
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

        private Task? _combinedTask;
        private readonly CancellationTokenSource _cts = new();
        private readonly AsyncAutoResetEvent _resumeReading = new();
        private readonly AsyncManualResetEvent _unpauseSignal = new(initialState: false);

        private bool _started;
        /// <summary>
        /// All the frames have been decoded *and displayed*
        /// </summary>
        private bool _videoEnded;
        private bool _loopingEnabled;
        private LoopRegion? _loopRegion;
        private TimeSpan? _seekRequest;

        private readonly Stopwatch _timer = new();
        private VideoFrameInfo _lastDisplayedFrameInfo;
        private double _frameTimer;

        private Clock _videoClock;
        private Clock _externalClock;

        public bool IsPlaying
            => _combinedTask is { IsCompleted: false } && !_videoEnded || (_audioSource?.IsPlaying ?? false);

        public TimeSpan Elapsed => TimeSpan.FromSeconds(GetSecondsElapsed());

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
            public readonly unsafe AVPacket* Packet;
            public readonly unsafe AVFrame* Frame;

            public readonly Channel<QueueItem<AVPacket>> PacketQueue;
            public readonly Channel<QueueItem<AVFrame>> FrameQueue;

            public int Serial;
            public TimeSpan? SeekRequest;
            public bool DecoderDoneSeeking;

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
                Packet = ffmpeg.av_packet_alloc();
                Frame = ffmpeg.av_frame_alloc();
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
                fixed (AVPacket** pkt = &Packet)
                {
                    ffmpeg.av_packet_free(pkt);
                }
                fixed (AVFrame** frame = &Frame)
                {
                    ffmpeg.av_frame_free(frame);
                }
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

            _recvPacket = ffmpeg.av_packet_alloc();
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
            CheckResult(ffmpeg.avcodec_open2(codecCtx, codec, null));

            if (codecCtx->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                return new StreamContext(stream, codecCtx, 1024, 2048);
            }

            return new StreamContext(stream, codecCtx, 256, 48);
        }

        public void Start()
        {
            if (_started) { return; }
            _started = true;
            _unpauseSignal.Set();
            var tasks = new List<Task>(5);
            tasks.Add(Task.Run(Read));
            if (_audio is { } && _audioSource is XAudio2AudioSource audioSource)
            {
                Debug.Assert(_audioPipe is not null);
                tasks.Add(Task.Run(() => Decode(_audio)));
                tasks.Add(Task.Run(() => ProcessAudio(
                    _audio,
                    _outAudioParams,
                    _audioPipe.Writer
                )));
                audioSource.Play(_audioPipe.Reader);
            }

            if (_video is { } && _videoBuffer is { })
            {
                tasks.Add(Task.Run(() => Decode(_video)));
                tasks.Add(Task.Run(() => ProcessVideo(_video, _videoBuffer.Writer)));
            }

            _combinedTask = Task.WhenAll(tasks);
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

                // When looping, display the loopStart frame right after loopEnd.
                // Otherwise there'd be a noticeable gap.
                return 0.0d;
            }

            Debug.Assert(_video is not null);
            YCbCrBufferReader frames = VideoFrames;
            while (frames.PeekFrame(out frame))
            {
                if (!_unpauseSignal.IsSet) { return false; }

                double time = _timer.Elapsed.TotalSeconds;
                if (_lastDisplayedFrameInfo.Serial != frame.Serial)
                {
                    if (_video.SeekRequest is TimeSpan)
                    {
                        _externalClock.Set(double.NaN, 0);
                        _video.SeekRequest = null;
                    }
                    else
                    {
                        frame.Dispose();
                        continue;
                    }
                }

                double duration = frameDuration(_lastDisplayedFrameInfo, frame.GetInfo());
                double targetDelay = CalcTargetDelay(duration);

                if (time < _frameTimer + targetDelay)
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
                bool end = FrameIsClosestTo(frame.GetInfo(), Duration);
                _videoEnded = end && !_loopingEnabled;
                return true;
            }

            return false;
        }

        private double CalcTargetDelay(double delay)
        {
            double diff = _videoClock.Get() - GetSecondsElapsed();
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

        private double GetSecondsElapsed()
        {
            return _video is not null
                ? _externalClock.Get()
                : _audioSource!.SecondsElapsed;
        }

        private void Seek(TimeSpan target)
        {
            _seekRequest = target;
            _resumeReading.Set();
        }

        private async Task Read()
        {
            int serial = 0;
            bool ignoreEof = false;
            QueueItem<AVPacket> packet = default;
            while (!_cts.IsCancellationRequested)
            {
                await _unpauseSignal.WaitAsync();

                if (_seekRequest is TimeSpan seekTarget)
                {
                    _seekRequest = null;
                    long timestamp = (long)Math.Round(seekTarget.TotalSeconds * ffmpeg.AV_TIME_BASE);
                    unsafe
                    {
                        CheckResult(ffmpeg.avformat_seek_file(
                            _formatContext, -1,
                            timestamp - 1 * ffmpeg.AV_TIME_BASE, timestamp, timestamp,
                            flags: 0
                        ));
                    }

                    serial++;

                    if (_audio is not null)
                    {
                        Interlocked.Increment(ref _audio.Serial);
                        _audio.DecoderDoneSeeking = false;
                        _audio.SeekRequest = seekTarget;
                    }
                    if (_video is not null)
                    {
                        Interlocked.Increment(ref _video.Serial);
                        _video.DecoderDoneSeeking = false;
                        _video.SeekRequest = seekTarget;
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
                    ret = ffmpeg.av_read_frame(_formatContext, _recvPacket);
                    packet.Value = *_recvPacket;
                    *_recvPacket = default;
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
                    // Flush packet
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
                    // EOF packet
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
                    await _resumeReading.WaitAsync();
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
                QueueItem<AVPacket> packet = await reader.ReadAsync();
                bool packetSent = false;
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
                        ret = ffmpeg.avcodec_receive_frame(ctx.CodecCtx, ctx.Frame);
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

                    unsafe
                    {
                        if (ctx.Frame->extended_data == &ctx.Frame->data)
                        {
                            // if extended_data points to data, the pointer will become
                            // invalid once the struct is moved in memory.
                            // It should be ok to set it to null in that case.
                            ctx.Frame->extended_data = null;
                        }
                        frame.Value = *ctx.Frame;
                        *ctx.Frame = default;
                    }

                    if (ctx.SeekRequest is TimeSpan seekTarget && !ctx.DecoderDoneSeeking)
                    {
                        if (FrameIsClosestTo(ctx, frame.Value, seekTarget))
                        {
                            ctx.DecoderDoneSeeking = true;
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
                            *ctx.Packet = packet.Value;
                            try
                            {
                                CheckResult(ffmpeg.avcodec_send_packet(ctx.CodecCtx, ctx.Packet));
                            }
                            finally
                            {
                                ffmpeg.av_packet_unref(ctx.Packet);
                            }
                        }
                        else
                        {
                            CheckResult(ffmpeg.avcodec_send_packet(ctx.CodecCtx, null));
                            goto receive_frames;
                        }
                    }
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
                            Seek(TimeSpan.Zero);
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

                if (_loopingEnabled && _loopRegion is LoopRegion loopRegion)
                {
                    if (FrameIsClosestTo(audio, frame.Value, loopRegion.End))
                    {
                        double loopEnd = loopRegion.End.TotalSeconds;
                        if (loopEnd > timestamp)
                        {
                            var actualDuration = TimeSpan.FromSeconds(loopEnd - timestamp);
                            int desiredSize = resampler.GetBufferSize(actualDuration);
                            bytesWritten = Math.Min(bufferReqs.SizeInBytes, desiredSize);
                        }
                        Seek(loopRegion.Start);
                    }
                    else if (audio.SeekRequest is not null
                        && FrameIsClosestTo(audio, frame.Value, loopRegion.Start, strict: true))
                    {
                        void trimStart(double newStart)
                        {
                            var excessDuration = TimeSpan.FromSeconds(newStart - timestamp);
                            int offset = resampler.GetBufferSize(excessDuration);
                            int newSize = bufferReqs.SizeInBytes - offset;
                            byte[] samples = ArrayPool<byte>.Shared.Rent(newSize);
                            buffer[offset..].CopyTo(samples);
                            writer.Advance(0);
                            Memory<byte> newBuffer = writer.GetMemory(newSize);
                            samples.CopyTo(newBuffer);
                            ArrayPool<byte>.Shared.Return(samples);
                            bytesWritten = newSize;
                        }

                        double loopStart = loopRegion.Start.TotalSeconds;
                        if (loopStart > timestamp)
                        {
                            trimStart(loopStart);
                        }
                    }
                }

                UnrefFrame(ref frame.Value);
                writer.Advance(bytesWritten);
                bytesBuffered += bytesWritten;
                if (bytesBuffered >= 16384)
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
                QueueItem<AVFrame> frame = await videoFrames.ReadAsync();
                if (frame.Serial != video.Serial)
                {
                    if (frame.Eof)
                    {
                        if (_loopingEnabled && _loopRegion is null)
                        {
                            Seek(TimeSpan.Zero);
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

        private bool FrameIsClosestTo(StreamContext ctx, in AVFrame frame, TimeSpan timestamp, bool strict = false)
        {
            double pts = frame.best_effort_timestamp * ctx.TimeBase.ToDouble();
            double duration = frame.width > 0
                ? _videoFrameDuration
                : (double)frame.nb_samples / frame.sample_rate;
            double ts = timestamp.TotalSeconds;
            bool containsTimestamp = Math.Abs(ts - pts) < duration;
            return strict
                ? containsTimestamp
                : containsTimestamp || Math.Abs(ts - pts - duration) < duration * 0.5d;
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
            _resumeReading.Set();
            _videoBuffer?.Writer.Clear();
            _video?.DestroyQueues();
            _audio?.DestroyQueues();

            if (_combinedTask is not null)
            {
                try
                {
                    _combinedTask.Wait();
                }
                catch (AggregateException e) when (e.InnerException is ChannelClosedException)
                {
                }
            }

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

            fixed (AVPacket** pkt = &_recvPacket)
            {
                ffmpeg.av_packet_free(pkt);
            }

            _fileStream.Dispose();
            _timer.Stop();
        }

        private static void NoVideoStream()
          => throw new InvalidOperationException("Media file does not contain a video stream.");
    }
}
