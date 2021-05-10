using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NitroSharp.Media.NullAudio;
using NitroSharp.Media.XAudio2;
using NitroSharp.Media.OpenAL;

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
                AudioBackend.OpenAL => new OpenALAudioDevice(audioParameters),
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
}
