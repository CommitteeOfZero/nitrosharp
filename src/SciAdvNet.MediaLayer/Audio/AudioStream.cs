using System;

namespace SciAdvNet.MediaLayer.Audio
{
    public abstract class AudioStream
    {
        protected AudioStream(int targetBitDepth, int targetSampleRate, int targetChannelCount)
        {
            TargetBitDepth = targetBitDepth;
            TargetSampleRate = targetSampleRate;
            TargetChannelCount = targetChannelCount;
        }

        public int TargetBitDepth { get; }
        public int TargetSampleRate { get; }
        public int TargetChannelCount { get; }

        public abstract void Seek(ulong positionInSamples);
        public abstract void Seek(TimeSpan timeCode);
        public abstract int Read(AudioBuffer buffer);
    }
}
