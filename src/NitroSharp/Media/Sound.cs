using System;
using System.IO;
using NitroSharp.NsScript;

namespace NitroSharp.Media
{
    internal sealed class Sound : Entity
    {
        private readonly NsAudioKind _kind;
        private VolumeAnimation? _volumeAnim;
        private readonly PooledAudioSource _audioSource;
        private bool _playbackStarted;

        public Sound(
            in ResolvedEntityPath path,
            NsAudioKind kind,
            Stream stream,
            AudioContext audioContext)
            : base(path)
        {
            _kind = kind;
            _audioSource = audioContext.RentAudioSource();
            Stream = new MediaStream(
                stream,
                graphicsDevice: null,
                _audioSource.Value,
                audioContext.Device.AudioParameters
            );
            Volume = 1.0f;
        }

        public Sound(in ResolvedEntityPath path, in EntitySaveData saveData)
            : base(in path, in saveData)
        {
        }

        public override EntityKind Kind => EntityKind.Sound;
        public override bool IsIdle
            => (!Stream.IsPlaying && _playbackStarted) || Volume == 0 && _volumeAnim is null;

        public MediaStream Stream { get; }

        public void Play()
        {
            _playbackStarted = true;
            Stream.Start();
        }

        public float Volume
        {
            get => _audioSource.Value.Volume;
            set => _audioSource.Value.Volume = value * GetVolumeMultiplier();
        }

        private float GetVolumeMultiplier() => _kind switch
        {
            NsAudioKind.BackgroundMusic => 0.7f,
            NsAudioKind.SoundEffect => 1.0f,
            NsAudioKind.Voice => 0.9f
        };

        public void Update(float dt)
        {
            _volumeAnim?.Update(dt);
            if (_volumeAnim is { HasCompleted: true })
            {
                _volumeAnim = null;
            }
        }

        public void AnimateVolume(float targetVolume, TimeSpan duration)
        {
            if (duration > TimeSpan.Zero)
            {
                _volumeAnim = new VolumeAnimation(this, Volume, targetVolume, duration);
            }
            else
            {
                Volume = targetVolume;
            }
        }


        public override void Dispose()
        {
            Stream.Dispose();
            _audioSource.Dispose();
        }
    }
}
