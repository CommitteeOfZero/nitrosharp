using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using NitroSharp.Media.XAudio2;

namespace NitroSharp.Media
{
    internal readonly struct PooledAudioSource : IDisposable
    {
        private readonly AudioSourcePool _pool;
        public readonly XAudio2AudioSource Value;

        public PooledAudioSource(AudioSourcePool pool, XAudio2AudioSource audioSource)
        {
            _pool = pool;
            Value = audioSource;
        }

        public void Dispose()
        {
            _pool.Return(Value);
        }
    }

    internal sealed class AudioSourcePool
    {
        private readonly ConcurrentQueue<XAudio2AudioSource> _freeSources;

        public AudioSourcePool(AudioDevice audioDevice, uint initialSize = 1)
        {
            AudioDevice = audioDevice;
            _freeSources = new ConcurrentQueue<XAudio2AudioSource>();

            VoiceAudioSource = AudioDevice.CreateAudioSource( bufferSize: 4400, bufferCount: 64);
            for (int i = 0; i < initialSize; i++)
            {
                _freeSources.Enqueue(AudioDevice.CreateAudioSource());
            }
        }

        public AudioDevice AudioDevice { get; }

        public XAudio2AudioSource VoiceAudioSource { get; }

        public PooledAudioSource Rent()
        {
            XAudio2AudioSource audioSource = _freeSources.TryDequeue(out XAudio2AudioSource? pooled)
                ? pooled
                : AudioDevice.CreateAudioSource();

            Debug.Assert(!audioSource.IsPlaying);
            Debug.Assert(!(audioSource.SecondsElapsed > 0));
            return new PooledAudioSource(this, audioSource);
        }

        public void Return(XAudio2AudioSource audioSource)
        {
            _freeSources.Enqueue(audioSource);
        }
    }
}
