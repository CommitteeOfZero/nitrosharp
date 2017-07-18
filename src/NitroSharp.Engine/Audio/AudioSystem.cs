using NitroSharp.Foundation;
using NitroSharp.Foundation.Audio;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;

namespace NitroSharp.Audio
{
    public sealed class AudioSystem : EntityProcessingSystem, IDisposable
    {
        private static uint VoiceBufferSize = 4400;

        private readonly AudioEngine _audioEngine;
        private uint _defaultBufferSize;

        private Dictionary<SoundComponent, AudioSource> _audioSources;
        private Queue<AudioSource> _freeAudioSources;

        public AudioSystem(AudioEngine audioEngine)
        {
            _audioEngine = audioEngine;
            _audioSources = new Dictionary<SoundComponent, AudioSource>();
            _freeAudioSources = new Queue<AudioSource>();

            _defaultBufferSize = (uint)(_audioEngine.SampleRate * _audioEngine.ChannelCount);
        }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(SoundComponent));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var sound = entity.GetComponent<SoundComponent>();
            var stream = sound.Source.Asset;
            var audioSource = GetFreeAudioSource(sound.Kind);
            stream.Seek(TimeSpan.Zero);
            audioSource.SetStream(stream);
            _audioSources[sound] = audioSource;
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var sound = entity.GetComponent<SoundComponent>();
            var audioSource = GetAssociatedSource(sound);
            audioSource.Volume = GetVolumeMultiplier(sound) * sound.Volume;

            if (sound.Volume > 0 && !audioSource.IsPlaying)
            {
                audioSource.Play();
            }
            else if (sound.Volume == 0 && audioSource.IsPlaying)
            {
                audioSource.Stop();
            }

            if (sound.Looping && !audioSource.CurrentStream.Looping)
            {
                if (sound.LoopEnd.TotalSeconds > 0)
                {
                    audioSource.CurrentStream.SetLoop(sound.LoopStart, sound.LoopEnd);
                }
                else
                {
                    audioSource.CurrentStream.SetLoop();
                }
            }

            sound.Elapsed = audioSource.PlaybackPosition;
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            var sound = entity.GetComponent<SoundComponent>();
            StopAndRemove(sound);
        }

        private void StopAndRemove(SoundComponent sound)
        {
            var audioSource = GetAssociatedSource(sound);
            audioSource.Stop();
            audioSource.SetStream(null);

            if (sound.Kind != AudioKind.Voice)
            {
                _audioSources.Remove(sound);
                _freeAudioSources.Enqueue(audioSource);
            }

            sound.Source.Dispose();
        }

        private AudioSource GetAssociatedSource(SoundComponent sound) => _audioSources[sound];
        private AudioSource GetFreeAudioSource(AudioKind audioKind)
        {
            uint bufferSize = audioKind == AudioKind.Voice ? VoiceBufferSize : _defaultBufferSize;
            if (_freeAudioSources.Count > 0)
            {
                return _freeAudioSources.Dequeue();
            }
            else
            {
                var source = _audioEngine.ResourceFactory.CreateAudioSource(bufferSize);
                if (audioKind == AudioKind.Voice)
                {
                    source.PreviewBufferSent += (sender, args) => CalculateAmplitude(sender as AudioSource, args);
                }

                return source;
            }
        }

        private void CalculateAmplitude(AudioSource source, AudioBuffer buffer)
        {
            var sound = _audioSources.First(x => x.Value == source).Key;

            int firstSample = Marshal.ReadInt16(buffer.StartPointer, 0);
            int secondSample = Marshal.ReadInt16(buffer.StartPointer, buffer.Position / 4);
            int thirdSample = Marshal.ReadInt16(buffer.StartPointer, buffer.Position / 4 + buffer.Position / 2);
            int fourthSample = Marshal.ReadInt16(buffer.StartPointer, buffer.Position - 2);

            double amplitude = (Math.Abs(firstSample) + Math.Abs(secondSample)
                + Math.Abs(thirdSample) + Math.Abs(fourthSample)) / 4.0d;

            Interlocked.Exchange(ref sound.Amplitude, (int)amplitude);
        }

        private static float GetVolumeMultiplier(SoundComponent sound)
        {
            switch (sound.Kind)
            {
                case AudioKind.BackgroundMusic:
                    return 0.6f;
                case AudioKind.SoundEffect:
                    return 1.0f;
                case AudioKind.Voice:
                default:
                    return 0.75f;
            }
        }

        public void Dispose()
        {
            _audioEngine.StopAllSources();
        }
    }
}
