using System;
using System.Collections.Concurrent;

namespace NitroSharp.Media
{
    public abstract class AudioSource : IDisposable
    {
        public const uint MaxQueuedBuffers = 16;

        protected ConcurrentQueue<IntPtr> _processedBufferPointers = new ConcurrentQueue<IntPtr>();

        public abstract IntPtr CurrentBuffer { get; }
        public abstract uint BuffersQueued { get; }
        public abstract uint TotalBuffersReferenced { get; }
        public abstract double PositionInCurrentBuffer { get; }

        public abstract float Volume { get; set; }

        public abstract bool TrySubmitBuffer(IntPtr data, uint size);

        public virtual bool TryDequeueProcessedBuffer(out IntPtr pointer)
        {
            return _processedBufferPointers.TryDequeue(out pointer);
        }

        public abstract void FlushBuffers();
        public abstract void Play();
        public abstract void Stop();
        public abstract void Dispose();
    }
}
