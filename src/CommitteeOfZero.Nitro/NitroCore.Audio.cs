using CommitteeOfZero.Nitro.Audio;
using CommitteeOfZero.NsScript;
using MoeGame.Framework;
using System;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        public override int GetSoundAmplitude()
        {
            var ampl = AudioSystem.Amplitude;
            if (ampl == 0)
            {
                //CurrentThread.Suspend(TimeSpan.FromMilliseconds(100));
            }

            return AudioSystem.Amplitude;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            var sound = new SoundComponent
            {
                AudioFile = fileName,
                Kind = (AudioKind)kind
            };

            _entities.Create(entityName, replace: true).WithComponent(sound);
        }

        public override void SetVolume(string entityName, TimeSpan duration, NsRational volume)
        {
            if (entityName == null)
                return;

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
                var animation = new FloatAnimation
                {
                    TargetComponent = sound,
                    PropertySetter = (c, v) => (c as SoundComponent).Volume = v,
                    InitialValue = sound.Volume,
                    FinalValue = volume,
                    Duration = duration
                };

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
                SetLoopingCore(e, loopStart, loopEnd);
            }
        }

        private void SetLoopingCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            var sound = entity.GetComponent<SoundComponent>();
            sound.LoopStart = loopEnd;
            sound.LoopEnd = loopEnd;
            sound.Looping = true;
        }
    }
}
