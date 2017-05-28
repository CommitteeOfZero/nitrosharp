using CommitteeOfZero.Nitro.Audio;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Animation;
using System;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        public override int GetSoundAmplitude()
        {
            return AudioSystem.Amplitude;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            var sound = new SoundComponent(fileName, (AudioKind)kind);
            _entities.Create(entityName, replace: true).WithComponent(sound);
        }

        public override void SetVolume(string entityName, TimeSpan duration, NsRational volume)
        {
            foreach (var e in _entities.Query(entityName))
            {
                SetVolumeCore(e, duration, volume);
            }
        }

        private void SetVolumeCore(Entity entity, TimeSpan duration, NsRational volume)
        {
            var sound = entity.GetComponent<SoundComponent>();
            volume = volume.Rebase(1.0f);
            if (duration > TimeSpan.Zero)
            {
                Action<Component, float> propertySetter = (c, v) => (c as SoundComponent).Volume = v;
                var animation = new FloatAnimation(sound, propertySetter, sound.Volume, volume, duration);
                entity.AddComponent(animation);
            }
            else
            {
                sound.Volume = volume;
            }
        }

        public override void ToggleLooping(string entityName, bool looping)
        {
            foreach (var e in _entities.Query(entityName))
            {
                ToggleLoopingCore(e, looping);
            }
        }

        private void ToggleLoopingCore(Entity entity, bool looping)
        {
            entity.GetComponent<SoundComponent>().Looping = looping;
        }

        public override void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            foreach (var e in _entities.Query(entityName))
            {
                SetLoopPointCore(e, loopStart, loopEnd);
            }
        }

        private void SetLoopPointCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            var sound = entity.GetComponent<SoundComponent>();
            sound.SetLoop(loopStart, loopEnd);
            sound.Looping = true;
        }
    }
}
