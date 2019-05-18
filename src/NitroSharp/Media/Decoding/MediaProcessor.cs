using System;
using FFmpeg.AutoGen;

namespace NitroSharp.Media.Decoding
{
    internal abstract class MediaProcessor : IDisposable
    {
        protected unsafe readonly AVStream* _avStream;
        private readonly UnmanagedMemoryPool _bufferPool;

        public unsafe MediaProcessor(AVStream* stream, uint bufferSize)
        {
            _avStream = stream;
            _bufferPool = new UnmanagedMemoryPool(bufferSize, BufferPoolSize);
        }

        protected abstract uint BufferPoolSize { get; }
        public UnmanagedMemoryPool BufferPool => _bufferPool;

        public abstract uint GetExpectedOutputBufferSize(ref AVFrame srcFrame);

        public abstract unsafe int ProcessFrame(ref AVFrame frame, ref PooledBuffer outBuffer);

        public virtual void Dispose()
        {
            _bufferPool.Dispose();
        }
    }
}
