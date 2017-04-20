using CommitteeOfZero.NsScript;
using MoeGame.Framework;
using System;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        public override int GetSoundAmplitude()
        {
            //CurrentThread.Suspend(TimeSpan.FromMilliseconds(100));
            return 0;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            _entities.Create(entityName, replace: true)
                .WithComponent(new SoundComponent { AudioFile = fileName });
        }

        public override void SetVolume(string entityName, TimeSpan duration, int volume)
        {
            if (entityName == null)
                return;

            foreach (var e in _entities.Query(entityName))
            {
                SetVolumeCore(e, duration, volume);
            }
        }

        private void SetVolumeCore(Entity entity, TimeSpan duration, int volume)
        {
            entity.GetComponent<SoundComponent>().Volume = volume;
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
