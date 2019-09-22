//using System;
//using System.Collections.Generic;
//using NitroSharp.Media.Decoding;
//using NitroSharp.Content;

//#nullable enable

//namespace NitroSharp.Media
//{
//    internal sealed class AudioSystem
//    {
//        private readonly AudioClipTable _audioClips;
//        private readonly AudioSourcePool _audioSourcePool;
//        private readonly ContentManager _content;

//        private readonly List<PlaybackState> _recycledPlaybackState;
//        private readonly List<AudioState> _recycledAudioState;

//        public AudioSystem(World world, ContentManager content, AudioSourcePool audioSourcePool)
//        {
//            _audioSourcePool = audioSourcePool;
//            _content = content;
//            _audioClips = world.AudioClips;
//            _recycledPlaybackState = new List<PlaybackState>();
//            _recycledAudioState = new List<AudioState>();
//        }

//        public void UpdateAudioSources()
//        {
//            foreach (var audioState in _recycledAudioState)
//            {
//                if (audioState.AudioSource != null)
//                {
//                    AudioRenderer.DeinitializeAudioState(audioState.AudioSource, _audioSourcePool);
//                }
//            }

//            Span<PlaybackState> playbackStates = _audioClips.PlaybackState.MutateAll();
//            Span<AudioState> audioStates = _audioClips.AudioState.MutateAll();
//            foreach (OldEntity entity in _audioClips.NewEntities)
//            {
//                ushort index = _audioClips.LookupIndex(entity);
//                AssetId asset = _audioClips.Asset.GetRef(index);
//                MediaPlaybackSession session = _content.GetMediaClip(asset);
//                ref PlaybackState playbackState = ref playbackStates[index];
//                PlaybackState.Initialize(ref playbackState, session);

//                if (!session.IsRunning)
//                {
//                    session.Start();
//                }

//                ref AudioState audioState = ref audioStates[index];
//                AudioRenderer.InitializeAudioState(ref audioState, ref playbackState, _audioSourcePool);

//                audioState.AudioSource.Play();
//                playbackState.Stopwatch.Start();
//            }

//            AudioRenderer.AdvanceAudio(_audioClips, playbackStates, audioStates);

//            //ReadOnlySpan<AudioState> audioState = _audioClips.AudioState.Enumerate();
//            //Span<double> amplitude = _audioClips.SoundAmplitude.MutateAll();
//            //for (int i = 0; i < audioState.Length; i++)
//            //{
//            //    amplitude[i] = AudioRenderer.CalculateAmplitude(audioState[i]);
//            //}
//        }
//    }
//}
