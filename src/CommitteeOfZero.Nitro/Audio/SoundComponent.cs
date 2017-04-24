using MoeGame.Framework;
using MoeGame.Framework.Content;
using System;

namespace CommitteeOfZero.Nitro.Audio
{
    public sealed class SoundComponent : Component
    {
        public AssetRef AudioFile { get; set; }
        public AudioKind Kind { get; set; }

        public TimeSpan LoopStart { get; set; }
        public TimeSpan LoopEnd { get; set; }
        public float Volume { get; set; }
        public bool Looping { get; set; }
        public bool RemoveOncePlayed { get; set; }
    }
}
