using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NitroSharp.Media.XAudio2;

namespace NitroSharp.Media
{
    internal readonly struct PooledAudioSource : IDisposable
    {
        private readonly AudioContext _pool;
        public readonly AudioSource Value;

        public PooledAudioSource(AudioContext pool, AudioSource audioSource)
        {
            _pool = pool;
            Value = audioSource;
        }

        public void Dispose()
        {
            _pool.ReturnAudioSource(Value);
        }
    }

    internal sealed class AudioContext : IAsyncDisposable
    {
        private readonly ConcurrentQueue<AudioSource> _freeSources;

        public AudioContext(AudioDevice device, uint initialSize = 1)
        {
            Device = device;
            _freeSources = new ConcurrentQueue<AudioSource>();

            VoiceAudioSource = Device.CreateAudioSource( bufferSize: 4400, bufferCount: 64);
            for (int i = 0; i < initialSize; i++)
            {
                _freeSources.Enqueue(Device.CreateAudioSource());
            }
        }

        public AudioDevice Device { get; }
        public AudioSource VoiceAudioSource { get; }

        public PooledAudioSource RentAudioSource()
        {
            AudioSource audioSource = _freeSources.TryDequeue(out AudioSource? pooled)
                ? pooled
                : Device.CreateAudioSource();
            audioSource.Volume = 1.0f;
            return new PooledAudioSource(this, audioSource);
        }

        public void ReturnAudioSource(AudioSource audioSource)
        {
            _freeSources.Enqueue(audioSource);
        }

        public ValueTask DisposeAsync() => Device.DisposeAsync();
    }
}
