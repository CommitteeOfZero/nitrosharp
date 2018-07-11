using NitroSharp.Media;
using NitroSharp.NsScript;
using System;
using NitroSharp.Animation;
using NitroSharp.Media.Decoding;
using System.IO;
using System.Linq;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        private AudioSourcePool AudioSourcePool => _game.AudioSourcePool;

        public override int GetSoundAmplitude(string characterName)
        {
            if (_dialogueState?.Voice?.CharacterName == characterName)
            {
                if (_entities.TryGet(VoiceEnityName, out var voiceEntity))
                {
                    var sound = voiceEntity.GetComponent<MediaComponent>();
                    return (int)sound.SoundAmplitude;
                }
            }

            return 0;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            if (!Content.TryGet<MediaPlaybackSession>(fileName, out var session))
            {
                string directory = Path.GetDirectoryName(fileName);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string searchPattern = nameWithoutExtension + "*";
                var assetId = Content.Search(directory, searchPattern).First();
                session = Content.Get<MediaPlaybackSession>(assetId);
            }

            var media = new MediaComponent(session, AudioSourcePool);
            _entities.Create(entityName, replace: true).WithComponent(media);
        }

        public override void LoadVideo(string entityName, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName)
        {
            var media = new MediaComponent(Content.Get<MediaPlaybackSession>(fileName), AudioSourcePool);
            media.Opacity = 1.0f;
            media.Priority = priority;
            media.EnableLooping = loop;

            _entities.Create(entityName, replace: true)
                .WithComponent(media)
                .WithPosition(x, y);
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
            var sound = entity.GetComponent<MediaComponent>();
            volume = volume.Rebase(1.0f);
            if (duration > TimeSpan.Zero)
            {
                Action<MediaComponent, float> setter = (s, v) => s.Volume = v;
                var animation = new FloatAnimation<MediaComponent>(sound, setter, sound.Volume, volume, duration);
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
            entity.GetComponent<MediaComponent>().EnableLooping = looping;
        }

        public override void SetLoopRegion(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            foreach (var e in _entities.Query(entityName))
            {
                SetLoopRegionCore(e, loopStart, loopEnd);
            }
        }

        private void SetLoopRegionCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            var sound = entity.GetComponent<MediaComponent>();
            sound.SetLoopRegion(loopStart, loopEnd);
            sound.EnableLooping = true;
        }

        public override int GetSoundDuration(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var sound = entity.GetComponent<MediaComponent>();
                return (int)sound.Duration.TotalMilliseconds;
            }

            return 0;
        }

        public override int GetTimeElapsed(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var sound = entity.GetComponent<MediaComponent>();
                return (int)sound.Elapsed.TotalMilliseconds;
            }

            return 0;
        }

        public override int GetTimeRemaining(string soundEntityName)
        {
            if (_entities.TryGet(soundEntityName, out var entity))
            {
                var sound = entity.GetComponent<MediaComponent>();
                TimeSpan duration = sound.Duration;
                return (int)(duration.TotalMilliseconds - sound.Elapsed.TotalMilliseconds);
            }

            return 0;
        }
    }
}
