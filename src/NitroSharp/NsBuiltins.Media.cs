using NitroSharp.Media;
using NitroSharp.NsScript;
using System;
using NitroSharp.Media.Decoding;
using System.IO;
using System.Linq;
using Veldrid;
using NitroSharp.Animation;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Content;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private AudioClipTable AudioClips => _world.AudioClips;
        private VideoClipTable VideoClips => _world.VideoClips;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MediaClipTable GetTable(Entity entity)
        {
            return entity.Kind == EntityKind.AudioClip
                ? (MediaClipTable)_world.AudioClips
                : _world.VideoClips;
        }

        public override int GetSoundAmplitude(string characterName)
        {
            //if (_dialogueState?.Voice?.CharacterName == characterName)
            //{
            //    if (_world.TryGetEntity(VoiceEnityName, out var voiceEntity))
            //    {
            //        var sound = voiceEntity.GetComponent<MediaComponent>();
            //        return (int)sound.SoundAmplitude;
            //    }
            //}

            return 0;
        }

        public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
        {
            var assetId = new AssetId(fileName);
            MediaPlaybackSession? session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
            if (session == null)
            {
                string directory = Path.GetDirectoryName(fileName);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string searchPattern = nameWithoutExtension + "*";
                assetId = Content.Search(directory, searchPattern).First();
                session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
            }

            if (session != null)
            {
                Entity entity = _world.CreateAudioClip(entityName, assetId, false);
                AudioClips.Duration.Set(entity, session.AudioStream!.Duration);
            }
        }

        public override void LoadVideo(string entityName, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName)
        {
            var assetId = new AssetId(fileName);
            MediaPlaybackSession? session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
            if (session != null)
            {
                RgbaFloat color = RgbaFloat.White;
                Entity entity = _world.CreateVideoClip(entityName, assetId, loop, priority, ref color);

                VideoStream stream = session.VideoStream!;
                VideoClips.Duration.Set(entity, stream.Duration);

                var bounds = new SizeF(stream.Width, stream.Height);
                VideoClips.Bounds.Set(entity, bounds);
            }
            //SetPosition(entity, x, y);
        }

        public override void WaitPlay(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                MediaClipTable table = GetTable(entity);
                TimeSpan duration = table.Duration.GetValue(entity);
                // TODO: nullable
                Interpreter.SuspendThread(MainThread!, duration);
            }
        }

        public override void SetVolume(string entityName, TimeSpan duration, NsRational volume)
        {
            foreach ((Entity e, string name) in _world.Query(entityName))
            {
                SetVolumeCore(e, duration, volume);
            }
        }

        private void SetVolumeCore(Entity entity, TimeSpan duration, NsRational finalVolume)
        {
            MediaClipTable table = GetTable(entity);
            ref float actual = ref table.Volume.Mutate(entity);
            finalVolume = finalVolume.Rebase(1.0f);
            if (duration > TimeSpan.Zero)
            {
                var animation = new VolumeAnimation(entity, duration);
                animation.InitialVolume = actual;
                animation.FinalVolume = finalVolume;
                _world.ActivateAnimation(animation);
            }
            else
            {
                actual = finalVolume;
            }
        }

        public override void ToggleLooping(string entityName, bool looping)
        {
            foreach ((Entity e, string name) in _world.Query(entityName))
            {
                ToggleLoopingCore(e, looping);
            }
        }

        private void ToggleLoopingCore(Entity entity, bool looping)
        {
            MediaClipTable table = GetTable(entity);
            table.LoopData.Mutate(entity).LoopingEnabled = looping;
        }

        public override void SetLoopRegion(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
            foreach ((Entity e, string name) in _world.Query(entityName))
            {
                SetLoopRegionCore(e, loopStart, loopEnd);
            }
        }

        private void SetLoopRegionCore(Entity entity, TimeSpan loopStart, TimeSpan loopEnd)
        {
            MediaClipTable table = GetTable(entity);
            ref MediaClipLoopData data = ref table.LoopData.Mutate(entity);
            data.LoopRegion = (loopStart, loopEnd);
            data.LoopingEnabled = true;
        }

        public override int GetSoundDuration(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                MediaClipTable table = GetTable(entity);
                return (int)table.Duration.GetValue(entity).TotalMilliseconds;
            }

            return 0;
        }

        public override int GetTimeElapsed(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                MediaClipTable table = GetTable(entity);
                return (int)table.Elapsed.GetValue(entity).TotalMilliseconds;
            }

            return 0;
        }

        public override int GetTimeRemaining(string soundEntityName)
        {
            if (_world.TryGetEntity(soundEntityName, out Entity entity))
            {
                MediaClipTable table = GetTable(entity);
                TimeSpan duration = table.Duration.GetValue(entity);
                TimeSpan elapsed = table.Elapsed.GetValue(entity);
                return (int)(duration.TotalMilliseconds - elapsed.TotalMilliseconds);
            }

            return 0;
        }
    }
}
