//using NitroSharp.Media;
//using NitroSharp.NsScript;
//using System;
//using NitroSharp.Media.Decoding;
//using System.IO;
//using System.Linq;
//using Veldrid;
//using NitroSharp.Animation;
//using System.Runtime.CompilerServices;
//using NitroSharp.Primitives;
//using NitroSharp.NsScript.Primitives;
//using NitroSharp.Content;
//using System.Diagnostics;

//#nullable enable

//namespace NitroSharp
//{
//    internal sealed partial class NsBuiltins
//    {
//        //private AudioClipTable AudioClips => _world.AudioClips;
//        //private VideoClipTable VideoClips => _world.VideoClips;

//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //private MediaClipTable GetTable(OldEntity entity)
//        //{
//        //    return entity.Kind == EntityKind.AudioClip
//        //        ? (MediaClipTable)_world.AudioClips
//        //        : _world.VideoClips;
//        //}

//        public override int GetSoundAmplitude(string characterName)
//        {
//            //if (_dialogueState?.Voice?.CharacterName == characterName)
//            //{
//            //    if (_world.TryGetEntity(VoiceEnityName, out var voiceEntity))
//            //    {
//            //        var sound = voiceEntity.GetComponent<MediaComponent>();
//            //        return (int)sound.SoundAmplitude;
//            //    }
//            //}

//            return 0;
//        }

//        //public override void LoadAudio(string entityName, NsAudioKind kind, string fileName)
//        //{
//        //    var sw = Stopwatch.StartNew();
//        //    var assetId = new AssetId(fileName);
//        //    MediaPlaybackSession? session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
//        //    if (session == null)
//        //    {
//        //        string directory = Path.GetDirectoryName(fileName);
//        //        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
//        //        string searchPattern = nameWithoutExtension + "*";
//        //        assetId = Content.Search(directory, searchPattern).First();
//        //        session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
//        //    }

//        //    if (session != null)
//        //    {
//        //        OldEntity entity = _world.CreateAudioClip(entityName, assetId, false);
//        //        AudioClips.Duration.Set(entity, session.AudioStream!.Duration);
//        //    }
//        //    sw.Stop();
//        //    Console.WriteLine(sw.Elapsed.TotalMilliseconds);
//        //}

//        //public override void LoadVideo(string entityName, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName)
//        //{
//        //    var assetId = new AssetId(fileName);
//        //    MediaPlaybackSession? session = Content.TryGetMediaClip(assetId, increaseRefCount: false);
//        //    if (session != null)
//        //    {
//        //        RgbaFloat color = RgbaFloat.White;
//        //        OldEntity entity = _world.CreateVideoClip(entityName, assetId, loop, priority, ref color);

//        //        VideoStream stream = session.VideoStream!;
//        //        VideoClips.Duration.Set(entity, stream.Duration);

//        //        var bounds = new SizeF(stream.Width, stream.Height);
//        //        VideoClips.Bounds.Set(entity, bounds);
//        //    }
//        //    //SetPosition(entity, x, y);
//        //}

//        //public override void WaitPlay(string entityName)
//        //{
//        //    if (_world.TryGetEntity(entityName, out OldEntity entity))
//        //    {
//        //        MediaClipTable table = GetTable(entity);
//        //        TimeSpan duration = table.Duration.GetRef(entity);
//        //        // TODO: nullable
//        //        Interpreter.SuspendThread(MainThread!, duration);
//        //    }
//        //}

//        //public override void SetVolume(string entityName, TimeSpan duration, NsRational volume)
//        //{
//        //    foreach ((OldEntity e, _) in QueryEntities(entityName))
//        //    {
//        //        SetVolumeCore(e, duration, volume);
//        //    }
//        //}

//        //private void SetVolumeCore(OldEntity entity, TimeSpan duration, NsRational finalVolume)
//        //{
//        //    MediaClipTable table = GetTable(entity);
//        //    ref float actual = ref table.Volume.GetRef(entity);
//        //    finalVolume = finalVolume.Rebase(1.0f);
//        //    if (duration > TimeSpan.Zero)
//        //    {
//        //        var animation = new VolumeAnimation(entity, duration)
//        //        {
//        //            InitialVolume = actual,
//        //            FinalVolume = finalVolume
//        //        };
//        //        _world.ActivateAnimation(animation);
//        //    }
//        //    else
//        //    {
//        //        actual = finalVolume;
//        //    }
//        //}

//        //public override void ToggleLooping(string entityName, bool looping)
//        //{
//        //    foreach ((OldEntity e, _) in QueryEntities(entityName))
//        //    {
//        //        ToggleLoopingCore(e, looping);
//        //    }
//        //}

//        //private void ToggleLoopingCore(OldEntity entity, bool looping)
//        //{
//        //    MediaClipTable table = GetTable(entity);
//        //    table.LoopData.GetRef(entity).LoopingEnabled = looping;
//        //}

//        //public override void SetLoopRegion(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
//        //{
//        //    foreach ((OldEntity e, _) in QueryEntities(entityName))
//        //    {
//        //        SetLoopRegionCore(e, loopStart, loopEnd);
//        //    }
//        //}

//        //private void SetLoopRegionCore(OldEntity entity, TimeSpan loopStart, TimeSpan loopEnd)
//        //{
//        //    MediaClipTable table = GetTable(entity);
//        //    ref MediaClipLoopData data = ref table.LoopData.GetRef(entity);
//        //    data.LoopRegion = (loopStart, loopEnd);
//        //    data.LoopingEnabled = true;
//        //}

//        //public override int GetSoundDuration(string entityName)
//        //{
//        //    if (_world.TryGetEntity(entityName, out OldEntity entity))
//        //    {
//        //        MediaClipTable table = GetTable(entity);
//        //        return (int)table.Duration.GetRef(entity).TotalMilliseconds;
//        //    }

//        //    return 0;
//        //}

//        //public override int GetTimeElapsed(string entityName)
//        //{
//        //    if (_world.TryGetEntity(entityName, out OldEntity entity))
//        //    {
//        //        MediaClipTable table = GetTable(entity);
//        //        return (int)table.Elapsed.GetRef(entity).TotalMilliseconds;
//        //    }

//        //    return 0;
//        //}

//        public override int GetTimeRemaining(string soundEntityName)
//        {
//            //if (_world.TryGetEntity(soundEntityName, out OldEntity entity))
//            //{
//            //    MediaClipTable table = GetTable(entity);
//            //    TimeSpan duration = table.Duration.GetRef(entity);
//            //    TimeSpan elapsed = table.Elapsed.GetRef(entity);
//            //    return (int)(duration.TotalMilliseconds - elapsed.TotalMilliseconds);
//            //}

//            return 0;
//        }
//    }
//}
