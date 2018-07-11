using System;
using System.Runtime.InteropServices;
using NitroSharp.Media.OpenAL;
using NitroSharp.Media.XAudio2;

namespace NitroSharp.Media
{
    public abstract class AudioDevice : IDisposable
    {
        public const int BitDepth = 16;

        protected AudioDevice(in AudioParameters audioParameters)
        {
            AudioParameters = audioParameters;
        }

        public AudioParameters AudioParameters { get; }

        public abstract AudioSource CreateAudioSource();
        public abstract void Dispose();

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
            return backend == AudioBackend.XAudio2
                ? (AudioDevice)new XAudio2AudioDevice(audioParameters)
                : new OpenALAudioDevice(audioParameters);
        }
    }

    public enum AudioBackend
    {
        XAudio2,
        OpenAL
    }
}
