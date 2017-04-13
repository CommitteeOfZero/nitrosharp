using System;

namespace MoeGame.Framework.Audio
{
    public abstract class AudioEngine : IDisposable
    {
        protected AudioEngine(int bitDepth, int sampleRate, int channelCount)
        {
            BitDepth = bitDepth;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
        }

        protected AudioEngine()
            : this(16, 44100, 2)
        {
        }

        public int BitDepth { get; }
        public int SampleRate { get; }
        public int ChannelCount { get; }

        public ResourceFactory ResourceFactory { get; protected set; }
        public abstract void Dispose();
    }
}
