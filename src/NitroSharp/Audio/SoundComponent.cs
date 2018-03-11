using System;
using System.Diagnostics;

namespace NitroSharp.Audio
{
    public sealed class SoundComponent : Component
    {
        public AudioKind Kind { get; }

        public bool IsPlaying { get; internal set; }
        public TimeSpan Elapsed { get; internal set; }
        public float Volume { get; set; }
        public TimeSpan LoopStart { get; private set; }
        public TimeSpan LoopEnd { get; private set; }
        public bool Looping { get; set; }
        public volatile int Amplitude;

        public void SetLoop(TimeSpan loopStart, TimeSpan loopEnd)
        {
            Debug.Assert(loopEnd > loopStart);

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }
    }
}
