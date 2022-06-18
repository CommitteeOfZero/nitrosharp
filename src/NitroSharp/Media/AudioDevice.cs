using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NitroSharp.Media.NullAudio;
using NitroSharp.Media.XAudio2;

namespace NitroSharp.Media
{
    internal abstract class AudioDevice : IAsyncDisposable
    {
        public const int BitDepth = 16;

        protected AudioDevice(in AudioParameters audioParameters)
        {
            AudioParameters = audioParameters;
        }

        public AudioParameters AudioParameters { get; }

        public abstract AudioSource CreateAudioSource(
            int bufferSize = 16 * 1024,
            int bufferCount = 16
        );

        public abstract ValueTask DisposeAsync();

        public static AudioBackend GetPlatformDefaultBackend()
        {
            return OperatingSystem.IsWindows()
                ? AudioBackend.XAudio2
                : AudioBackend.Null;
        }

        public static bool IsBackendAvailable(AudioBackend backend)
        {
            if (backend == AudioBackend.XAudio2)
            {
                return OperatingSystem.IsWindows();
            }

            return true;
        }

        public static AudioDevice Create(AudioBackend backend, in AudioParameters audioParameters)
        {
            return backend switch
            {
                AudioBackend.Null => new NullAudioDevice(audioParameters),
                AudioBackend.XAudio2 => new XAudio2AudioDevice(audioParameters),
                _ => throw new NotImplementedException($"Backend '{backend}' is not implemented")
            };
        }
    }

    public enum AudioBackend
    {
        Null,
        XAudio2,
        OpenAL
    }

    public readonly struct AudioParameters : IEquatable<AudioParameters>
    {
        public static readonly AudioParameters Default = new(ChannelLayout.Stereo, 44100);

        public readonly ChannelLayout ChannelLayout;
        public readonly uint SampleRate;

        public AudioParameters(ChannelLayout channelLayout, uint sampleRate)
        {
            SampleRate = sampleRate;
            ChannelLayout = channelLayout;
        }

        public int ChannelCount => ChannelLayout == ChannelLayout.Mono ? 1 : 2;

        public override bool Equals(object? obj) => obj is AudioParameters other && Equals(other);
        public bool Equals(AudioParameters other)
        {
            return ChannelLayout == other.ChannelLayout && SampleRate == other.SampleRate;
        }

        public override int GetHashCode() => HashCode.Combine(ChannelLayout, SampleRate);

        public static bool operator ==(AudioParameters left, AudioParameters right) => left.Equals(right);
        public static bool operator !=(AudioParameters left, AudioParameters right) => !left.Equals(right);
    }

    public enum ChannelLayout
    {
        Mono,
        Stereo
    }
}
