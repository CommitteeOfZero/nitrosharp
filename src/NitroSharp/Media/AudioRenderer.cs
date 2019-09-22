//using System;
//using System.Collections.Generic;
//using NitroSharp.Media.Decoding;

//namespace NitroSharp.Media
//{
//    internal static class AudioRenderer
//    {
//        private static readonly Dictionary<IntPtr, MediaFrame> _submittedAudioFrames = new Dictionary<IntPtr, MediaFrame>();

//        public static void AdvanceAudio<T>(
//            T mediaClips,
//            Span<PlaybackState> playbackState,
//            Span<AudioState> audioState)
//            where T : EntityTable, MediaClipTable
//        {
//            if (mediaClips.EntryCount == 0) { return; }
//            ReadOnlySpan<MediaClipLoopData> loopData = mediaClips.LoopData.Enumerate();
//            ReadOnlySpan<float> volume = mediaClips.Volume.Enumerate();
//            Span<TimeSpan> elapsed = mediaClips.Elapsed.MutateAll();
//            int count = playbackState.Length;
//            for (int i = 0; i < count; i++)
//            {
//                ref AudioState state = ref audioState[i];
//                if (state.AudioSource != null)
//                {
//                    state.AudioSource.Volume = volume[i];
//                }
//            }

//            for (int i = 0; i < count; i++)
//            {
//                ref PlaybackState state = ref playbackState[i];
//                float vol = volume[i];
//                if (state.HasAudio && vol > 0)
//                {
//                    AdvanceAudio(
//                        ref state, ref audioState[i],
//                        loopData[i], ref elapsed[i]);
//                }
//            }
//        }

//        public static void InitializeAudioState(
//            ref AudioState audioState,
//            ref PlaybackState playbackState,
//            AudioSourcePool audioSourcePool)
//        {
//            audioState.AudioStream = playbackState.PlaybackSession.AudioStream;
//            audioState.SampleQueue = playbackState.PlaybackSession.AudioBufferQueue;
//            audioState.AudioSource = audioSourcePool.Rent();
//        }

//        public static void DeinitializeAudioState(
//            AudioSource audioSource,
//            AudioSourcePool audioSourcePool)
//        {
//            audioSource.Stop();
//            audioSource.FlushBuffers();

//            bool gotBuffer = false;
//            IntPtr bufferPtr = default;
//            while (audioSource.BuffersQueued > 0
//                || (gotBuffer = audioSource.TryDequeueProcessedBuffer(out bufferPtr)))
//            {
//                if (gotBuffer)
//                {
//                    MediaFrame frame = _submittedAudioFrames[bufferPtr];
//                    frame.Free();
//                }
//            }

//            audioSourcePool.Return(audioSource);
//        }

//        public static void AdvanceAudio(
//            ref PlaybackState playbackState,
//            ref AudioState audioState,
//            in MediaClipLoopData loopData,
//            ref TimeSpan elapsed)
//        {
//            AudioSource audioSource = audioState.AudioSource;
//            while (audioSource.TryDequeueProcessedBuffer(out IntPtr pointer))
//            {
//                MediaFrame frame = _submittedAudioFrames[pointer];
//                frame.Free();
//            }

//            MediaFrameQueue<MediaFrame> bufferQueue = audioState.SampleQueue;
//            while (bufferQueue.TryPeek(out MediaFrame audio))
//            {
//                if (Synchonization.ShouldSkipFrame(ref audio, ref playbackState, loopData))
//                {
//                    bufferQueue.Take();
//                    audio.Free();
//                    continue;
//                }

//                if (audioSource.TrySubmitBuffer(audio.Buffer.Data, audio.Buffer.Size))
//                {
//                    _submittedAudioFrames[audio.Buffer.Data] = audio;
//                    bufferQueue.Take();
//                }
//                else
//                {
//                    break;
//                }
//            }


//            elapsed = TimeSpan.FromSeconds(Synchonization.GetPlaybackPosition(ref playbackState, audioState));
//        }

//        public static double CalculateAmplitude(in AudioState audioState)
//        {
//            IntPtr sampleBuffer = _submittedAudioFrames[audioState.AudioSource.CurrentBuffer].Buffer.Data;
//            if (sampleBuffer != IntPtr.Zero)
//            {
//                MediaFrame frame = _submittedAudioFrames[sampleBuffer];
//                int bufferSize = (int)frame.Buffer.Size;
//                unsafe
//                {
//                    var span = new ReadOnlySpan<short>(sampleBuffer.ToPointer(), bufferSize / 2);
//                    int firstSample = span[0];
//                    int secondSample = span[span.Length / 4];
//                    int thirdSample = span[span.Length / 4 + span.Length / 2];
//                    int fourthSample = span[span.Length - 2];

//                    double amplitude =
//                        (Math.Abs(firstSample) + Math.Abs(secondSample)
//                        + Math.Abs(thirdSample) + Math.Abs(fourthSample)) / 4.0d;

//                    return amplitude;
//                }
//            }

//            return 0;
//        }

//        public static double GetPlaybackPosition(in AudioState audioState)
//        {
//            AudioSource audioSource = audioState.AudioSource;
//            IntPtr currentBuffer = audioSource.CurrentBuffer;
//            if (currentBuffer != IntPtr.Zero && _submittedAudioFrames.TryGetValue(currentBuffer, out MediaFrame frame))
//            {
//                double value = frame.PresentationTimestamp + audioSource.PositionInCurrentBuffer;
//                return value;
//            }

//            return 0;
//        }
//    }
//}
