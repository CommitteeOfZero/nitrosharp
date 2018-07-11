using System;
using System.Threading.Tasks;
using System.Threading;
using FFmpeg.AutoGen;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace NitroSharp.Media.Decoding
{
    internal sealed partial class MediaProcessingPipeline : IDisposable
    {
        private static readonly uint AvFramePoolSize = 32;
        private static readonly uint AvFrameSize = (uint)Unsafe.SizeOf<AVFrame>();

        private static readonly int ErrorEAgain = ffmpeg.AVERROR(ffmpeg.EAGAIN);

        private readonly Channel<PooledStruct<AVPacket>> _rawPackets;
        private readonly Channel<PooledStruct<AVFrame>> _decodedFrames;
        private readonly Channel<MediaFrame> _processedFrames;

        private readonly DecodingSession _decodingSession;
        private readonly MediaProcessor _processor;
        private readonly UnmanagedMemoryPool _avFramePool;
        private readonly DecodedFrameReceiver _frameReceiver;

        private Task _decoding;
        private Task _processing;
        private CancellationTokenSource _cts;
        private volatile int _disposed;

        private double? _seekTarget;
        private bool SeekingRequested
        {
            get => _seekTarget.HasValue;
            set => _seekTarget = null;
        }

        public MediaProcessingPipeline(DecodingSession decodingSession, MediaProcessor processor)
        {
            _decodingSession = decodingSession;
            _processor = processor;

            var channelOptions = new UnboundedChannelOptions()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false
            };

            _rawPackets = Channel.CreateUnbounded<PooledStruct<AVPacket>>(channelOptions);
            _decodedFrames = Channel.CreateUnbounded<PooledStruct<AVFrame>>(channelOptions);
            _processedFrames = Channel.CreateUnbounded<MediaFrame>(channelOptions);

            _avFramePool = new UnmanagedMemoryPool(AvFrameSize, AvFramePoolSize, clearMemory: true);
            _frameReceiver = new DecodedFrameReceiver(this);
            Output = new MediaFrameQueue<MediaFrame>(_processedFrames);
        }

        public MediaFrameQueue<MediaFrame> Output { get; private set; }
        public Task Completion { get; private set; }

        private AVRational StreamTimebase => _decodingSession.StreamTimebase;

        public void Start(CancellationTokenSource cts)
        {
            Debug.Assert(_decoding == null);

            _cts = cts;
            _decoding = Task.Run(() => Decode(cts));
            _processing = Task.Run(() => ProcessFrames(cts));
        }

        public ValueTask SendPacketAsync(ref PooledStruct<AVPacket> packet)
        {
            return _rawPackets.Writer.WriteAsync(packet);
        }

        public void Seek(double timeInSeconds)
        {
            _seekTarget = timeInSeconds;
        }

        private async Task Decode(CancellationTokenSource cts)
        {
            var output = _decodedFrames.Writer;
            while (!cts.IsCancellationRequested)
            {
                var input = _rawPackets.Reader;
                PooledStruct<AVPacket> pooledPacket = await input.ReadAsync();
                if (cts.IsCancellationRequested)
                {
                    pooledPacket.Free();
                    return;
                }

                if (pooledPacket.IsNull)
                {
                    // EOF situation.
                    // 1) Receive frames (in case the decoder has any)
                    await _frameReceiver.ReceiveFramesFromDecoder(cts.Token);
                    // 2) Drain the decoder by sending nullptr instead of an actual packet
                    int error = _decodingSession.TrySendPacket(IntPtr.Zero);
                    await _frameReceiver.ReceiveFramesFromDecoder(cts.Token);
                    // 3) Reset the decoder and propagate EOF by submitting a null frame to the frame queue.
                    _decodingSession.FlushBuffers();
                    PooledStruct<AVFrame> eofFrame = default;
                    await output.WriteAsync(eofFrame, cts.Token);
                    continue;
                }

                var packet = new AVPacket();
                FFmpegUtil.PacketMoveRef(ref packet, ref pooledPacket.AsRef());
                pooledPacket.Free();

                try
                {
                    int error;
                    do
                    {
                        if (cts.IsCancellationRequested) { return; }
                        error = _decodingSession.TrySendPacket(ref packet);
                        if (error == ErrorEAgain)
                        {
                            await _frameReceiver.ReceiveFramesFromDecoder(cts.Token);
                        }
                    } while (error != 0);
                }
                finally
                {
                    FFmpegUtil.UnrefBuffers(ref packet);
                }
            }
        }

        private struct FrameProcessingContext
        {
            private readonly MediaProcessingPipeline _parent;

            public PooledBuffer Buffer;
            public double Pts;
            public double FrameDuration;

            public FrameProcessingContext(MediaProcessingPipeline parent)
            {
                _parent = parent;
                Buffer = default;
                Pts = double.NaN;
                FrameDuration = 0;
            }

            public bool BufferHasData => Buffer.Size > 0;
            public bool BufferHasEnoughSpace(uint spaceRequired) => Buffer.FreeSpace >= spaceRequired;
            public void AdvancePosition(uint offset) => Buffer.Position += offset;

            public void SetPtsAndDuration(ref AVFrame sourceFrame, AVRational streamTimeBase)
            {
                if (double.IsNaN(Pts))
                {
                    Pts = _parent.ToSeconds(sourceFrame.best_effort_timestamp);
                }

                FrameDuration = _parent.ToSeconds(sourceFrame.pts) - Pts
                    + _parent.ToSeconds(sourceFrame.pkt_duration);
            }

            public void SetBuffer(ref PooledBuffer buffer)
            {
                Buffer = buffer;
                Pts = double.NaN;
                FrameDuration = 0;
                Buffer.Position = 0;
            }

            public void SetEof()
            {
                Buffer = default;
                FrameDuration = 0;
                Pts = double.NaN;
            }
        }

        private async Task ProcessFrames(CancellationTokenSource cts)
        {
            var context = new FrameProcessingContext(this);
            bool frameSubmitted = false;
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var input = _decodedFrames.Reader;
                    PooledStruct<AVFrame> srcFrame = default;
                    bool taken = false;
                    do
                    {
                        srcFrame = await input.ReadAsync(CancellationToken.None);
                        if (cts.IsCancellationRequested) { return; }
                        if (srcFrame.IsNull) { break; }
                        taken = ShouldTakeFrame(ref srcFrame.AsRef());
                        if (!taken)
                        {
                            srcFrame.Free();
                        }
                    } while (!taken);

                    if (srcFrame.IsNull)
                    {
                        // EOF situation.
                        // 1) Submit everything we have
                        if (context.BufferHasData)
                        {
                            await SubmitProcessedFrame(ref context, cts.Token);
                        }
                        // 2) Propagate EOF by submitting a null frame.
                        context.SetEof();
                        await SubmitProcessedFrame(ref context, cts.Token);
                        continue;
                    }

                    try
                    {
                        uint bytesRequired = _processor.GetExpectedOutputBufferSize(ref srcFrame.AsRef());
                        Debug.Assert(_processor.BufferPool.ChunkSize >= bytesRequired);
                        // If the current buffer is full *or* if seeking is requested, we need to
                        // submit the current buffer and get a new one.
                        if (!context.BufferHasEnoughSpace(bytesRequired) || SeekingRequested)
                        {
                            SeekingRequested = false;
                            if (context.BufferHasData)
                            {
                                await SubmitProcessedFrame(ref context, cts.Token);
                                frameSubmitted = true;
                            }

                            UnmanagedMemoryPool bufferPool = _processor.BufferPool;
                            IntPtr pointer = await bufferPool.RentChunkAsync();
                            var nextBuffer = new PooledBuffer(pointer, bufferPool.ChunkSize, bufferPool);
                            context.SetBuffer(ref nextBuffer);
                            frameSubmitted = false;

                            if (cts.IsCancellationRequested) { return; }
                        }

                        uint bytesWritten = (uint)_processor.ProcessFrame(ref srcFrame.AsRef(), ref context.Buffer);
                        context.AdvancePosition(bytesWritten);
                        context.SetPtsAndDuration(ref srcFrame.AsRef(), _decodingSession.StreamTimebase);
                    }
                    finally
                    {
                        srcFrame.Free();
                    }
                }
            }
            finally
            {
                if (!frameSubmitted)
                {
                    context.Buffer.Free();
                }
            }
        }

        private bool ShouldTakeFrame(ref AVFrame frame)
        {
            if (!SeekingRequested)
            {
                return true;
            }

            double pts = ToSeconds(frame.best_effort_timestamp);
            double duration = ToSeconds(frame.pkt_duration);
            double target = _seekTarget.Value;
            return target >= pts && target <= pts + duration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ToSeconds(long ts)
        {
            return FFmpegUtil.RebaseTimestamp(ts, StreamTimebase);
        }

        private ValueTask SubmitProcessedFrame(ref FrameProcessingContext state, CancellationToken ct)
        {
            var output = _processedFrames.Writer;
            state.Buffer.Size = state.Buffer.Position;
            var processedFrame = new MediaFrame(state.Buffer, state.Pts, state.FrameDuration);
            return output.WriteAsync(processedFrame, ct);
        }

        private double GetPresentationTimestamp(ref AVFrame frame)
        {
            long pts = frame.best_effort_timestamp;
            return FFmpegUtil.RebaseTimestamp(pts, _decodingSession.StreamTimebase);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                _cts.Cancel();
                Completion = Complete();
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
                await Task.WhenAll(_decoding, _processing);
            }
            finally
            {
                FlushProcessedFrames();
                FlushDecodedFrames();
                DestroyFramePool();
            }
        }

        public void FlushDecodedFrames()
        {
            while (_decodedFrames.Reader.TryRead(out PooledStruct<AVFrame> avFrame))
            {
                avFrame.Free();
            }
        }

        private void FlushProcessedFrames()
        {
            while (_processedFrames.Reader.TryRead(out MediaFrame processedFrame))
            {
                processedFrame.Free();
            }
        }

        private void DestroyFramePool()
        {
            foreach (ref AVFrame frame in _avFramePool.AsSpan<AVFrame>())
            {
                FFmpegUtil.UnrefBuffers(ref frame);
            }

            _avFramePool.Dispose();
        }
    }
}
