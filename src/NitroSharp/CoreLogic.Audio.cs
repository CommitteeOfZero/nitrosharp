using NitroSharp.Audio;
using NitroSharp.NsScript;
using System;
using NitroSharp.Animation;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        public override int GetSoundAmplitude(string characterName)
        {
            return 0;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            var sound = new SoundComponent();
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

        public override int GetSoundDuration(string entityName)
        {
            return 100000;
        }

        public override int GetTimeElapsed(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var sound = entity.GetComponent<SoundComponent>();
                return (int)sound.Elapsed.TotalMilliseconds;
            }

            return 0;
        }

        public override int GetTimeRemaining(string soundEntityName)
        {
            return 80000;
        }
    }
}
