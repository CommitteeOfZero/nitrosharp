using System;

namespace ProjectHoppy
{
    public class SoundComponent : Component
    {
        public bool Loaded { get; set; }
        public bool Playing { get; set; }
        public TimeSpan LoopStart { get; set; }
        public TimeSpan LoopEnd { get; set; }
        public float Volume { get; set; }
        public bool Looping { get; internal set; }
    }
}
