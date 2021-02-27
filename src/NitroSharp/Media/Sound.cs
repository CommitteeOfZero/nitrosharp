using System;
using System.IO;

namespace NitroSharp.Media
{
    internal sealed class Sound : Entity
    {
        private VolumeAnimation? _volumeAnim;
        private readonly PooledAudioSource _audioSource;

        public Sound(
            in ResolvedEntityPath path,
            Stream stream,
            AudioContext audioContext)
            : base(path)
        {
            _audioSource = audioContext.RentAudioSource();
            Stream = new MediaStream(
                stream,
                graphicsDevice: null,
                _audioSource.Value,
                audioContext.Device.AudioParameters
            );
        }

        public Sound(in ResolvedEntityPath path, in EntitySaveData saveData)
            : base(in path, in saveData)
        {
        }

        public override EntityKind Kind => EntityKind.Sound;
        public override bool IsIdle
            => !Stream.IsPlaying || Volume == 0 && _volumeAnim is null;

        public MediaStream Stream { get; }

        public float Volume
        {
            get => _audioSource.Value.Volume;
            set => _audioSource.Value.Volume = value;
        }

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
