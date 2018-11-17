using System;
using NitroSharp.Content;
using NitroSharp.Media.Decoding;

namespace NitroSharp.Media
{
    internal sealed class AudioSystem
    {
        private readonly AudioClipTable _audioClips;
        private readonly VideoClipTable _videoClips;
        private readonly AudioSourcePool _audioSourcePool;
        private readonly ContentManager _content;

        public AudioSystem(World world, ContentManager content, AudioSourcePool audioSourcePool)
        {
            _audioSourcePool = audioSourcePool;
            _content = content;
            _audioClips = world.AudioClips;
            _videoClips = world.VideoClips;
        }

        public void UpdateAudioSources()
        {
            var added = _audioClips.AddedEntities;
            foreach (Entity e in added)
            {
                AssetId asset = _audioClips.Asset.GetValue(e);
                var session = _content.Get<MediaPlaybackSession>(asset).Asset;
                ref PlaybackState playbackState = ref _audioClips.PlaybackState.Mutate(e);
                PlaybackState.Initialize(ref playbackState, session);

                if (!session.IsRunning)
                {
                    session.Start();
                }

                ref AudioState audioState = ref _audioClips.AudioState.Mutate(e);
                AudioRenderer.InitializeAudioState(ref audioState, ref playbackState, _audioSourcePool);

                audioState.AudioSource.Play();
                playbackState.Stopwatch.Start();
            }

            var removed = _audioClips.AudioState.RecycledComponents;
            foreach (var audioState in removed)
            {
                if (audioState.AudioSource != null)
                {
                    AudioRenderer.DeinitializeAudioState(audioState.AudioSource, _audioSourcePool);
                }
            }

            added = _videoClips.AddedEntities;
            foreach (Entity e in added)
            {
                ref PlaybackState playbackState = ref _videoClips.PlaybackState.Mutate(e);
                if (playbackState.HasAudio)
                {
                    ref AudioState audioState = ref _videoClips.AudioState.Mutate(e);
                    AudioRenderer.InitializeAudioState(ref audioState, ref playbackState, _audioSourcePool);
                    audioState.AudioSource.Play();
                }
            }

            removed = _videoClips.AudioState.RecycledComponents;
            foreach (var audioState in removed)
            {
                if (audioState.AudioSource != null)
                {
                    AudioRenderer.DeinitializeAudioState(audioState.AudioSource, _audioSourcePool);
                }
            }

            AdvanceAudio(_audioClips);

            //ReadOnlySpan<AudioState> audioState = _audioClips.AudioState.Enumerate();
            //Span<double> amplitude = _audioClips.SoundAmplitude.MutateAll();
            //for (int i = 0; i < audioState.Length; i++)
            //{
            //    amplitude[i] = AudioRenderer.CalculateAmplitude(audioState[i]);
            //}

            AdvanceAudio(_videoClips);
        }

        private void AdvanceAudio<T>(T table) where T : EntityTable, MediaClipTable
        {
            if (table.ColumnsUsed > 0)
            {
                Span<PlaybackState> playbackState = table.PlaybackState.MutateAll();
                Span<AudioState> audioState = table.AudioState.MutateAll();
                ReadOnlySpan<MediaClipLoopData> loopData = table.LoopData.Enumerate();
                ReadOnlySpan<float> volume = table.Volume.Enumerate();
                Span<TimeSpan> elapsed = table.Elapsed.MutateAll();

                int count = playbackState.Length;
                for (int i = 0; i < count; i++)
                {
                    ref AudioState state = ref audioState[i];
                    if (state.AudioSource != null)
                    {
                        state.AudioSource.Volume = volume[i];
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    ref PlaybackState state = ref playbackState[i];
                    float vol = volume[i];
                    if (state.HasAudio && vol > 0)
                    {
                        AudioRenderer.AdvanceAudio(
                            ref state, ref audioState[i],
                            loopData[i], vol, ref elapsed[i]);
                    }
                }
            }
        }
    }
}
