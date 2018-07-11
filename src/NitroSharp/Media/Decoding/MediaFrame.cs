using System;

namespace NitroSharp.Media.Decoding
{
    public readonly struct MediaFrame
    {
        public readonly PooledBuffer Buffer;
        public readonly double PresentationTimestamp;
        public readonly double DurationInSeconds;

        public MediaFrame(PooledBuffer buffer, double presentationTimestamp, double duration)
        {
            Buffer = buffer;
            PresentationTimestamp = presentationTimestamp;
            DurationInSeconds = duration;
        }

        public bool IsEofFrame => double.IsNaN(PresentationTimestamp);

        public bool ContainsTimestamp(double timestamp)
        {
            return timestamp >= PresentationTimestamp && timestamp <= PresentationTimestamp + DurationInSeconds;
        }

        public void Free()
        {
            Buffer.Free();
        }
    }
}
