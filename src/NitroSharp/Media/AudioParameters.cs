using System;
using NitroSharp.Utilities;

namespace NitroSharp.Media
{
    public readonly struct AudioParameters : IEquatable<AudioParameters>
    {
        public static readonly AudioParameters Default = new AudioParameters(ChannelLayout.Stereo, 44100);

        public readonly ChannelLayout ChannelLayout;
        public readonly uint SampleRate;

        public AudioParameters(ChannelLayout channelLayout, uint sampleRate)
        {
            SampleRate = sampleRate;
            ChannelLayout = channelLayout;
        }

        public int ChannelCount => ChannelLayout == ChannelLayout.Mono ? 1 : 2;

        public override bool Equals(object obj) => obj is AudioParameters other && Equals(other);
        public bool Equals(AudioParameters other)
        {
            return ChannelLayout == other.ChannelLayout && SampleRate == other.SampleRate;
        }

        public override int GetHashCode() => HashHelper.Combine((int)ChannelLayout, SampleRate.GetHashCode());

        public static bool operator ==(AudioParameters left, AudioParameters right) => left.Equals(right);
        public static bool operator !=(AudioParameters left, AudioParameters right) => !(left.Equals(right));
    }

    public enum ChannelLayout
    {
        Mono,
        Stereo
    }
}
