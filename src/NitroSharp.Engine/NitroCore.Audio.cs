using NitroSharp.Audio;
using NitroSharp.NsScript;
using NitroSharp.Foundation;
using NitroSharp.Foundation.Animation;
using System;
using NitroSharp.Foundation.Audio;
using System.IO;
using System.Linq;

namespace NitroSharp
{
    public sealed partial class NitroCore
    {
        public override int GetSoundAmplitude(string characterName)
        {
            if (_currentDialogueLine?.Voice?.CharacterName == characterName)
            {
                if (_entities.TryGet(VoiceEntityName, out var voiceEntity))
                {
                    var soundComponent = voiceEntity.GetComponent<SoundComponent>();
                    return soundComponent.IsPlaying ? soundComponent.Amplitude : 0;
                }
            }

            return 0;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            if (!_content.TryGet<AudioStream>(fileName, out var audioStream))
            {
                string directory = Path.GetDirectoryName(fileName);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string searchPattern = nameWithoutExtension + "*";
                var assetId = _content.Search(directory, searchPattern).First();
                audioStream = _content.Get<AudioStream>(assetId);
            }

            var sound = new SoundComponent(audioStream, (AudioKind)kind);
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

        public override int GetTimeRemaining(string soundEntityName)
        {
            if (_entities.TryGet(soundEntityName, out var entity))
            {
                var sound = entity.GetComponent<SoundComponent>();
                var duration = sound.Source.Asset.Duration;
                return (int)(duration.TotalMilliseconds - sound.Elapsed.TotalMilliseconds);
            }

            return 0;
        }
    }
}
