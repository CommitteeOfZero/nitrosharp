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
            AudioSourcePool audioSourcePool)
            : base(path)
        {
            _audioSource = audioSourcePool.Rent();
            Stream = new MediaStream(
                stream,
                graphicsDevice: null,
                _audioSource.Value,
                audioSourcePool.AudioDevice.AudioParameters
            );
        }

        public Sound(in ResolvedEntityPath path, in EntitySaveData saveData)
            : base(in path, in saveData)
        {
        }

        public override EntityKind Kind => EntityKind.Sound;
        public override bool IsIdle => !Stream.IsPlaying;

        public MediaStream Stream { get; }

        public float Volume
        {
            get => Stream.AudioSource.Volume;
            set => Stream.AudioSource.Volume = value;
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
