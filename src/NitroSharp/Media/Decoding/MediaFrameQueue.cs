using System.Collections.Generic;
using System.Threading.Channels;

namespace NitroSharp.Media.Decoding
{
    public sealed class MediaFrameQueue<T> where T : struct
    {
        private readonly Queue<T> _queue;
        private readonly ChannelReader<T> _channelReader;

        public MediaFrameQueue(ChannelReader<T> channelReader)
        {
            _channelReader = channelReader;
            _queue = new Queue<T>();
        }

        public bool TryPeek(out T frame)
        {
            if (_queue.Count > 0)
            {
                frame = _queue.Peek();
                return true;
            }

            if (_channelReader.TryRead(out frame))
            {
                _queue.Enqueue(frame);
                return true;
            }

            return false;
        }

        public bool TryTake(out T frame)
        {
            if (_queue.Count > 0)
            {
                frame = Take();
                return true;
            }
            else
            {
                return _channelReader.TryRead(out frame);
            }
        }

        public T Take() => _queue.Dequeue();
        public void Clear() => _queue.Clear();
    }
}
