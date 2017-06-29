using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Audio;
using CommitteeOfZero.NitroSharp.Foundation.Content;
using System;
using System.Diagnostics;

namespace CommitteeOfZero.NitroSharp.Audio
{
    public sealed class SoundComponent : Component
    {
        public SoundComponent(AssetRef<AudioStream> source, AudioKind kind)
        {
            Source = source;
            Kind = kind;
            Volume = 1.0f;
        }

        public AssetRef<AudioStream> Source { get; }
        public AudioKind Kind { get; }

        public float Volume { get; set; }
        public TimeSpan LoopStart { get; private set; }
        public TimeSpan LoopEnd { get; private set; }
        public bool Looping { get; set; }


        public void SetLoop(TimeSpan loopStart, TimeSpan loopEnd)
        {
            Debug.Assert(loopEnd > loopStart);

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public override void OnRemoved()
        {
            Source.Dispose();
        }

        public override string ToString() => $"Sound '{Source}', kind = {Kind.ToString()}";
    }
}
