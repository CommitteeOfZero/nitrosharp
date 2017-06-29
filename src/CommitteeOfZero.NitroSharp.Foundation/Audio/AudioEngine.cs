using System;
using System.Collections.Generic;

namespace CommitteeOfZero.NitroSharp.Foundation.Audio
{
    public abstract class AudioEngine : IDisposable
    {
        private readonly HashSet<AudioSource> _audioSources;

        protected AudioEngine(int bitDepth, int sampleRate, int channelCount)
        {
            BitDepth = bitDepth;
            SampleRate = sampleRate;
            ChannelCount = channelCount;

            _audioSources = new HashSet<AudioSource>();
        }

        protected AudioEngine()
            : this(16, 44100, 2)
        {
        }

        public int BitDepth { get; }
        public int SampleRate { get; }
        public int ChannelCount { get; }

        public ResourceFactory ResourceFactory { get; protected set; }

        public void StopAllVoices()
        {
            foreach (var voice in _audioSources)
            {
                voice.Stop();
            }
        }

        public virtual void Dispose()
        {
            foreach (var source in _audioSources)
            {
                source.Dispose();
            }
        }

        internal void RegisterAudioSource(AudioSource source)
        {
            _audioSources.Add(source);
        }
    }
}
