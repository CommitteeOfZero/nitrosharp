using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NitroSharp.Media.Decoding;

namespace NitroSharp.Media
{
    internal static class AudioRenderer
    {
        private static readonly Dictionary<IntPtr, MediaFrame> _submittedAudioFrames = new Dictionary<IntPtr, MediaFrame>();

        public static void InitializeAudioState(
            ref AudioState audioState,
            ref PlaybackState playbackState,
            AudioSourcePool audioSourcePool)
        {
            audioState.AudioStream = playbackState.PlaybackSession.AudioStream;
            audioState.SampleQueue = playbackState.PlaybackSession.AudioBufferQueue;
            audioState.AudioSource = audioSourcePool.Rent();
        }

        public static void DeinitializeAudioState(
            AudioSource audioSource,
            AudioSourcePool audioSourcePool)
        {
            audioSource.Stop();
            audioSource.FlushBuffers();

            bool gotBuffer = false;
            IntPtr bufferPtr = default;
            while (audioSource.BuffersQueued > 0
                || (gotBuffer = audioSource.TryDequeueProcessedBuffer(out bufferPtr)))
            {
                if (gotBuffer)
                {
                    MediaFrame frame = _submittedAudioFrames[bufferPtr];
                    frame.Free();
                }
            }

            audioSourcePool.Return(audioSource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AdvanceAudio(
            ref PlaybackState playbackState,
            ref AudioState audioState,
            in MediaClipLoopData loopData,
            float volume,
            ref TimeSpan elapsed)
        {
            AudioSource audioSource = audioState.AudioSource;
            while (audioSource.TryDequeueProcessedBuffer(out IntPtr pointer))
            {
                MediaFrame frame = _submittedAudioFrames[pointer];
                frame.Free();
            }

            MediaFrameQueue<MediaFrame> bufferQueue = audioState.SampleQueue;
            while (bufferQueue.TryPeek(out MediaFrame audio))
            {
                if (Synchonization.ShouldSkipFrame(ref audio, ref playbackState, loopData))
                {
                    bufferQueue.Take();
                    audio.Free();
                    continue;
                }

                if (audioSource.TrySubmitBuffer(audio.Buffer.Data, audio.Buffer.Size))
                {
                    _submittedAudioFrames[audio.Buffer.Data] = audio;
                    bufferQueue.Take();
                }
                else
                {
                    break;
                }
            }


            elapsed = TimeSpan.FromSeconds(Synchonization.GetPlaybackPosition(ref playbackState, audioState));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalculateAmplitude(in AudioState audioState)
        {
            IntPtr sampleBuffer = _submittedAudioFrames[audioState.AudioSource.CurrentBuffer].Buffer.Data;
            if (sampleBuffer != IntPtr.Zero)
            {
                MediaFrame frame = _submittedAudioFrames[sampleBuffer];
                int bufferSize = (int)frame.Buffer.Size;
                unsafe
                {
                    var span = new ReadOnlySpan<short>(sampleBuffer.ToPointer(), bufferSize / 2);
                    int firstSample = span[0];
                    int secondSample = span[span.Length / 4];
                    int thirdSample = span[span.Length / 4 + span.Length / 2];
                    int fourthSample = span[span.Length - 2];

                    double amplitude =
                        (Math.Abs(firstSample) + Math.Abs(secondSample)
                        + Math.Abs(thirdSample) + Math.Abs(fourthSample)) / 4.0d;

                    return amplitude;
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetPlaybackPosition(in AudioState audioState)
        {
            AudioSource audioSource = audioState.AudioSource;
            IntPtr currentBuffer = audioSource.CurrentBuffer;
            if (currentBuffer != IntPtr.Zero && _submittedAudioFrames.TryGetValue(currentBuffer, out MediaFrame frame))
            {
                double value = frame.PresentationTimestamp + audioSource.PositionInCurrentBuffer;
                return value;
            }

            return 0;
        }
    }
}
