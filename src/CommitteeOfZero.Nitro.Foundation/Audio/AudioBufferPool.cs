using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CommitteeOfZero.Nitro.Foundation.Audio
{
    public class AudioBufferPool : IDisposable
    {
        private readonly AudioBuffer[] _buffers;
        private readonly BufferBlock<AudioBuffer> _freeBuffers;

        public AudioBufferPool(int bufferCount, int bufferSize)
        {
            _buffers = new AudioBuffer[bufferCount];
            _freeBuffers = new BufferBlock<AudioBuffer>();
            for (int i = 0; i < bufferCount; i++)
            {
                var buffer = new AudioBuffer(i, bufferSize);
                _buffers[i] = buffer;
                _freeBuffers.Post(buffer);
            }
        }

        public Task<AudioBuffer> TakeAsync(CancellationToken cancellationToken)
        {
            return _freeBuffers.ReceiveAsync(cancellationToken);
        }

        public void Release(AudioBuffer buffer)
        {
            _freeBuffers.Post(buffer);
        }

        public void Dispose()
        {
            _freeBuffers.Complete();
            foreach (var buffer in _buffers)
            {
                buffer.Dispose();
            }
        }
    }
}
