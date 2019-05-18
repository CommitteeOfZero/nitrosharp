using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using NitroSharp.Primitives;

#nullable enable

namespace NitroSharp.Media.Decoding
{
    public sealed class MediaPlaybackSession : IDisposable
    {
        private static readonly uint PacketPoolSize = 32;

        private readonly ProcessingContext[] _processingContexts;
        private readonly UnmanagedMemoryPool _packetPool;
        private ProcessingContext _audioProcessingContext;
        private ProcessingContext _videoProcessingContext;

        private Task? _readingPackets;
        private volatile bool _running;
        private CancellationTokenSource? _cts;

        private double? _seekTarget;
        private volatile int _disposed;

        private bool SeekingRequested
        {
            get => _seekTarget.HasValue;
            set => _seekTarget = null;
        }

        private struct ProcessingContext
        {
            public uint StreamId;
            public DecodingSession DecodingSession;
            public MediaProcessor Processor;
            public MediaProcessingPipeline Pipeline;
        }

        public MediaPlaybackSession(
            MediaContainer mediaContainer,
            VideoStream? videoStream,
            AudioStream? audioStream,
            in MediaProcessingOptions options)
        {
            Container = mediaContainer;
            _processingContexts = new ProcessingContext[mediaContainer.MediaStreams.Length];
            _packetPool = new UnmanagedMemoryPool((uint)Unsafe.SizeOf<AVPacket>(), PacketPoolSize, clearMemory: true);

            VideoStream = videoStream ?? mediaContainer.BestVideoStream;
            AudioStream = audioStream ?? mediaContainer.BestAudioStream;
            mediaContainer.SelectStreams(VideoStream, AudioStream);
            if (VideoStream != null)
            {
                OpenStream(VideoStream, null, options.FrameConverter, options.OutputVideoResolution, out _videoProcessingContext);
                VideoFrameQueue = _videoProcessingContext.Pipeline.Output;
            }
            if (AudioStream != null)
            {
                OpenStream(AudioStream, options.OutputAudioParameters, null, null, out _audioProcessingContext);
                AudioBufferQueue = _audioProcessingContext.Pipeline.Output;
            }
        }

        public MediaPlaybackSession(MediaContainer mediaContainer, VideoStream videoStream, in MediaProcessingOptions options)
            : this(mediaContainer, videoStream, null, options)
        {
        }

        public MediaPlaybackSession(MediaContainer mediaContainer, AudioStream videoStream, in MediaProcessingOptions options)
            : this(mediaContainer, null, videoStream, options)
        {
        }

        public MediaPlaybackSession(MediaContainer mediaContainer, in MediaProcessingOptions options)
            : this(mediaContainer, null, null, options)
        {
        }

        public MediaContainer Container { get; }
        public VideoStream? VideoStream { get; }
        public AudioStream? AudioStream { get; }
        public MediaFrameQueue<MediaFrame>? VideoFrameQueue { get; private set; }
        public MediaFrameQueue<MediaFrame>? AudioBufferQueue { get; private set; }

        public bool IsRunning => _running;
        public Task? Completion { get; private set; }

        private unsafe void OpenStream(
            MediaStream stream, AudioParameters? audioParameters, VideoFrameConverter? frameConverter,
            Size? videoResolution, out ProcessingContext processingContext)
        {
            var decodingSession = new DecodingSession(stream.AvStream);
            AVStream* avStream = stream.AvStream;
            var processor = stream.Kind == MediaStreamKind.Audio
                ? (MediaProcessor)new AudioProcessor(avStream, new Resampler(decodingSession.CodecContext, audioParameters.Value))
                : new VideoProcessor(frameConverter, avStream, videoResolution);

            var processingPipeline = new MediaProcessingPipeline(decodingSession, processor);
            processingContext = new ProcessingContext
            {
                StreamId = (uint)avStream->index,
                DecodingSession = decodingSession,
                Processor = processor,
                Pipeline = processingPipeline
            };

            _processingContexts[avStream->index] = processingContext;
        }

        public void Start()
        {
            if (_running)
            {
                throw new InvalidOperationException("Start() has already been called.");
            }

            _cts = new CancellationTokenSource();
            foreach (var stream in _processingContexts)
            {
                stream.Pipeline?.Start(_cts);
            }

            AudioBufferQueue = _audioProcessingContext.Pipeline?.Output;
            VideoFrameQueue = _videoProcessingContext.Pipeline?.Output;
            _running = true;
            _readingPackets = Task.Run(ReadPackets);
        }

        public void Seek(TimeSpan time) => Seek(time.TotalSeconds);

        public void Seek(double timeInSeconds)
        {
            _seekTarget = timeInSeconds;
            _videoProcessingContext.Pipeline?.Seek(timeInSeconds);
            _audioProcessingContext.Pipeline?.Seek(timeInSeconds);

            if (_readingPackets?.IsCompleted == true)
            {
                _readingPackets = Task.Run(ReadPackets);
            }
        }

        private async Task ReadPackets()
        {
            Debug.Assert(_cts != null);
            bool eof = false;
            while (!_cts.IsCancellationRequested && !eof)
            {
                if (_seekTarget.HasValue)
                {
                    Container.Seek(TimeSpan.FromSeconds(_seekTarget.Value));
                    SeekingRequested = false;
                }

                IntPtr chunk = await _packetPool.RentChunkAsync();
                var pooledPacket = new PooledStruct<AVPacket>(chunk, _packetPool);
                bool submitted = false;
                try
                {
                    int result = Container.ReadFrame(ref pooledPacket.AsRef());
                    if (result == ffmpeg.AVERROR_EOF)
                    {
                        eof = true;
                        pooledPacket.Free();
                        pooledPacket = default;
                        for (int i = 0; i < _processingContexts.Length; i++)
                        {
                            await SubmitPacket(ref pooledPacket, i);
                        }
                        submitted = true;
                    }
                    else
                    {
                        int streamIndex = pooledPacket.AsRef().stream_index;
                        if (_processingContexts[streamIndex].Pipeline != null)
                        {
                            await SubmitPacket(ref pooledPacket, streamIndex);
                            submitted = true;
                        }
                    }
                }
                finally
                {
                    if (!submitted)
                    {
                        pooledPacket.Free();
                    }
                }
            }
        }

        private ValueTask SubmitPacket(ref PooledStruct<AVPacket> packet, int streamIndex)
        {
            ref ProcessingContext ctx = ref _processingContexts[streamIndex];
            MediaProcessingPipeline pipeline = ctx.Pipeline;
            if (pipeline == null) { return default; }
            return pipeline.SendPacketAsync(ref packet);
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0 && _readingPackets != null)
            {
                Completion = Task.Run(async () =>
                {
                    Stop();
                    await Complete();
                });
            }
            else
            {
                Completion = Task.FromResult(0);
            }
        }

        private async Task Complete()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (ProcessingContext ctx in _processingContexts)
                {
                    MediaProcessingPipeline pipeline = ctx.Pipeline;
                    if (pipeline != null)
                    {
                        pipeline.Dispose();
                        tasks.Add(pipeline.Completion);
                    }
                }

                if (_readingPackets != null)
                {
                    tasks.Add(_readingPackets);
                }
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                foreach (ProcessingContext ctx in _processingContexts)
                {
                    MediaProcessor processor = ctx.Processor;
                    if (processor != null)
                    {
                        processor.Dispose();
                        ctx.DecodingSession.Dispose();
                    }
                }

                Container.Dispose();
            }
        }

        // TODO: unused method
        private void DestroyPacketPool()
        {
            foreach (ref AVPacket packet in _packetPool.AsSpan<AVPacket>())
            {
                FFmpegUtil.UnrefBuffers(ref packet);
            }

            _packetPool.Dispose();
        }
    }
}
