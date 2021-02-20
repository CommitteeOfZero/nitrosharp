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
        public readonly XAudio2AudioSource Value;

        public PooledAudioSource(AudioContext pool, XAudio2AudioSource audioSource)
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
        private readonly ConcurrentQueue<XAudio2AudioSource> _freeSources;

        public AudioContext(AudioDevice device, uint initialSize = 1)
        {
            Device = device;
            _freeSources = new ConcurrentQueue<XAudio2AudioSource>();

            VoiceAudioSource = Device.CreateAudioSource( bufferSize: 4400, bufferCount: 64);
            for (int i = 0; i < initialSize; i++)
            {
                _freeSources.Enqueue(Device.CreateAudioSource());
            }
        }

        public AudioDevice Device { get; }
        public XAudio2AudioSource VoiceAudioSource { get; }

        public PooledAudioSource RentAudioSource()
        {
            XAudio2AudioSource audioSource = _freeSources.TryDequeue(out XAudio2AudioSource? pooled)
                ? pooled
                : Device.CreateAudioSource();

            Debug.Assert(!audioSource.IsPlaying);
            Debug.Assert(!(audioSource.SecondsElapsed > 0));
            return new PooledAudioSource(this, audioSource);
        }

        public void ReturnAudioSource(XAudio2AudioSource audioSource)
        {
            _freeSources.Enqueue(audioSource);
        }

        public ValueTask DisposeAsync() => Device.DisposeAsync();
    }
}
