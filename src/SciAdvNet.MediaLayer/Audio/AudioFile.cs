using System;

namespace SciAdvNet.MediaLayer.Audio
{
    public abstract class AudioFile : IDisposable
    {
        public int SampleRate { get; protected set; }
        public int Channels { get; protected set; }
        public long TotalSamples { get; protected set; }

        public abstract int ReadSamples(float[] buffer, int offset, int count);
        public abstract void Dispose();
    }
}
