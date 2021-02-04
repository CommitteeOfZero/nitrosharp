using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

        public abstract XAudio2AudioSource CreateAudioSource(
            int bufferSize = 16 * 1024,
            int bufferCount = 16
        );

        public abstract ValueTask DisposeAsync();

        public static AudioDevice CreatePlatformDefault(in AudioParameters audioParameters)
        {
            return Create(GetPlatformDefaultBackend(), audioParameters);
        }

        public static AudioBackend GetPlatformDefaultBackend()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? AudioBackend.XAudio2
                : AudioBackend.OpenAL;
        }

        public static bool IsBackendAvailable(AudioBackend backend)
        {
            if (backend == AudioBackend.XAudio2)
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }

            return true;
        }

        public static AudioDevice Create(AudioBackend backend, in AudioParameters audioParameters)
        {
            return new XAudio2AudioDevice(audioParameters);
        }
    }

    public enum AudioBackend
    {
        XAudio2,
        OpenAL
    }
}
