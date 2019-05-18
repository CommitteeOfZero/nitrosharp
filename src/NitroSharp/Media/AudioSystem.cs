using System;
using System.Collections.Generic;
using NitroSharp.Media.Decoding;
using NitroSharp.Content;

#nullable enable

namespace NitroSharp.Media
{
    internal sealed class AudioSystem
    {
        private readonly AudioClipTable _audioClips;
        private readonly AudioSourcePool _audioSourcePool;
        private readonly ContentManager _content;

        private PlaybackState[] _playbackState;
        private AudioState[] _audioState;
        private readonly List<PlaybackState> _recycledPlaybackState;
        private readonly List<AudioState> _recycledAudioState;

        public AudioSystem(World world, ContentManager content, AudioSourcePool audioSourcePool)
        {
            _audioSourcePool = audioSourcePool;
            _content = content;
            _audioClips = world.AudioClips;
            _playbackState = new PlaybackState[_audioClips.Capacity];
            _audioState = new AudioState[_audioClips.Capacity];
            _recycledPlaybackState = new List<PlaybackState>();
            _recycledAudioState = new List<AudioState>();
        }

        public void UpdateAudioSources()
        {
            _audioClips.RearrangeSystemComponents(ref _playbackState, _recycledPlaybackState);
            _audioClips.RearrangeSystemComponents(ref _audioState, _recycledAudioState);

            foreach (var audioState in _recycledAudioState)
            {
                if (audioState.AudioSource != null)
                {
                    AudioRenderer.DeinitializeAudioState(audioState.AudioSource, _audioSourcePool);
                }
            }

            foreach (Entity entity in _audioClips.NewEntities)
            {
                ushort index = _audioClips.LookupIndex(entity);
                AssetId asset = _audioClips.Asset.GetValue(index);
                MediaPlaybackSession session = _content.GetMediaClip(asset);
                ref PlaybackState playbackState = ref _playbackState[index];
                PlaybackState.Initialize(ref playbackState, session);

                if (!session.IsRunning)
                {
                    session.Start();
                }

                ref AudioState audioState = ref _audioState[index];
                AudioRenderer.InitializeAudioState(ref audioState, ref playbackState, _audioSourcePool);

                audioState.AudioSource.Play();
                playbackState.Stopwatch.Start();
            }

            AudioRenderer.AdvanceAudio(
                _audioClips,
                _playbackState.AsSpan(0, _audioClips.EntryCount),
                _audioState.AsSpan(0, _audioClips.EntryCount));

            //ReadOnlySpan<AudioState> audioState = _audioClips.AudioState.Enumerate();
            //Span<double> amplitude = _audioClips.SoundAmplitude.MutateAll();
            //for (int i = 0; i < audioState.Length; i++)
            //{
            //    amplitude[i] = AudioRenderer.CalculateAmplitude(audioState[i]);
            //}
        }
    }
}
