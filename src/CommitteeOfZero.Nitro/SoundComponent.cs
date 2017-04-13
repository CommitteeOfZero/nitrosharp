using MoeGame.Framework;
using MoeGame.Framework.Content;
using System;

namespace CommitteeOfZero.Nitro
{
    public class SoundComponent : Component
    {
        public AssetRef AudioFile { get; set; }

        public bool Loaded { get; set; }
        public bool Playing { get; set; }
        public TimeSpan LoopStart { get; set; }
        public TimeSpan LoopEnd { get; set; }
        public float Volume { get; set; }
        public bool Looping { get; internal set; }
    }
}
