using System;
using System.IO;
using System.Threading.Tasks;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp
{
    internal partial class Builtins
    {
        public override void LoadAudio(in EntityPath entityPath, NsAudioKind kind, string fileName)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath path)
                && _ctx.Content.TryOpenStream(fileName) is Stream stream)
            {
                World.Add(new Sound(path, stream, _ctx.AudioSourcePool));
            }
        }

        public override void PlayVideo(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            bool loop, bool alpha,
            string source)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.TryOpenStream(source) is Stream fs)
            {
                Video video = World.Add(new Video(
                    resolvedPath, priority,
                    _renderCtx, _ctx.AudioSourcePool,
                    fs, alpha
                )).WithPosition(_renderCtx, x, y);
                video.Stream.ToggleLooping(loop);
                video.Stream.Start();
            }
        }

        public override void SetLoopRegion(in EntityPath entityPath, TimeSpan loopStart, TimeSpan loopEnd)
        {
            if (Get(entityPath) is Sound sound)
            {
                sound.Stream.SetLoopRegion(new LoopRegion(loopStart, loopEnd));
                sound.Stream.ToggleLooping(true);
            }
        }

        public override void ToggleLooping(EntityQuery query, bool enable)
        {
            foreach (Sound sound in Query<Sound>(query))
            {
                sound.Stream.ToggleLooping(enable);
            }
        }

        public override void SetVolume(EntityQuery query, TimeSpan duration, NsRational volume)
        {
            foreach (Sound sound in Query<Sound>(query))
            {
                sound.AnimateVolume(volume.Rebase(1.0f), duration);
            }
        }

        public override void WaitPlay(in EntityPath entityPath)
        {
            if (Get(entityPath) is object)
            {
                _ctx.Wait(CurrentThread,
                    WaitCondition.EntityIdle, null, new EntityQuery(entityPath.Value)
                );
            }
        }

        public override int GetSoundAmplitude(string characterName)
        {
            if (_ctx.GetVoice(characterName) is MediaStream voice)
            {
                ReadOnlySpan<short> samples = voice.AudioSource.GetCurrentBuffer();
                if (samples.Length > 0)
                {
                    int firstSample = samples[0];
                    int secondSample = samples[samples.Length / 4];
                    int thirdSample = samples[samples.Length / 4 + samples.Length / 2];
                    int fourthSample = samples[^2];
                    double amplitude =
                        (Math.Abs(firstSample) + Math.Abs(secondSample)
                            + Math.Abs(thirdSample) + Math.Abs(fourthSample)) / 4.0d;
                    return (int)amplitude;
                }
            }

            return 0;
        }

        public override int GetSoundDuration(in EntityPath entityPath)
        {
            return Get(entityPath) is Sound sound ?
                (int)sound.Stream.Duration.TotalMilliseconds
                : 0;
        }

        public override int GetTimeElapsed(in EntityPath entityPath) => 0;
        public override int GetTimeRemaining(EntityQuery query) => 0;
    }
}
