using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace NitroSharp.Media
{
    internal sealed class AudioSourcePool : IDisposable
    {
        private readonly AudioDevice _audioDevice;
        private readonly ConcurrentQueue<AudioSource> _freeSources;

        public AudioSourcePool(AudioDevice audioDevice, uint initialSize = 8)
        {
            _audioDevice = audioDevice;
            _freeSources = new ConcurrentQueue<AudioSource>();

            for (int i = 0; i < initialSize; i++)
            {
                _freeSources.Enqueue(_audioDevice.CreateAudioSource());
            }
        }

        public AudioSource Rent()
        {
            var audioSource = _freeSources.TryDequeue(out AudioSource pooled)
                ? pooled
                : _audioDevice.CreateAudioSource();

            Debug.Assert(audioSource.TotalBuffersReferenced == 0);
            return audioSource;
        }

        public void Return(AudioSource audioSource)
        {
            audioSource.FlushBuffers();
            _freeSources.Enqueue(audioSource);
        }

        public void Dispose()
        {
            while (_freeSources.TryDequeue(out AudioSource audioSource))
            {
                audioSource.Dispose();
            }
        }
    }
}
