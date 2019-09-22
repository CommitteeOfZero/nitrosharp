//using System;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;
//using NitroSharp.Media.Decoding;

//namespace NitroSharp.Media
//{
//    internal static class Synchonization
//    {
//        private const double SyncThresholdMin = 0.04d;
//        private const double SyncThresholdMax = 0.1d;

//        public static void Seek(ref PlaybackState playbackState, double timestamp)
//        {
//            playbackState.PlaybackSession.Seek(timestamp);
//            playbackState.SeekTarget = timestamp;
//            playbackState.Seeking = true;
//        }

//        public static bool ShouldSkipFrame(ref MediaFrame frame, ref PlaybackState state, in MediaClipLoopData loopData)
//        {
//            if (state.Seeking)
//            {
//                bool reachedTargetTime = frame.ContainsTimestamp(state.SeekTarget);
//                if (reachedTargetTime)
//                {
//                    state.Seeking = false;
//                }

//                return !reachedTargetTime;
//            }

//            if (loopData.LoopingEnabled)
//            {
//                if (loopData.LoopRegion.HasValue)
//                {
//                    (TimeSpan loopStart, TimeSpan loopEnd) = loopData.LoopRegion.Value;
//                    if (frame.ContainsTimestamp(loopEnd.TotalSeconds))
//                    {
//                        Seek(ref state, loopStart.TotalSeconds);
//                    }
//                }
//                else
//                {
//                    if (frame.IsEofFrame)
//                    {
//                        Seek(ref state, 0);
//                    }
//                }
//            }

//            return frame.IsEofFrame;
//        }

//        /// <summary>
//        /// Returns true if it's time to take the next frame out of the queue and display it.
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool ShouldTakeFrame(
//            ref PlaybackState playbackState, ref VideoState videoState, double delay)
//        {
//            double diff = GetTime(ref playbackState) - (videoState.NextRefreshTime + delay);
//            return diff >= 0;
//        }

//        /// <summary>
//        /// Calculates the delay between two frames using their PTS values.
//        /// </summary>
//        public static double CalculateDelay(
//            ref PlaybackState playbackState,
//            ref VideoState videoState, in AudioState audioState,
//            double pts, double prevPts)
//        {
//            double delay = pts - prevPts;
//            double diff = videoState.VideoClock.Get() - GetPlaybackPosition(ref playbackState, audioState);
//            double syncThreshold = Math.Max(SyncThresholdMin, Math.Min(SyncThresholdMax, diff));

//            if (diff <= -syncThreshold) // the video is behind
//            {
//                delay = Math.Max(0, delay + diff);
//            }
//            else if (diff >= syncThreshold && delay > 0.1d)
//            {
//                delay += diff;
//            }
//            else if (diff >= syncThreshold)
//            {
//                delay = 2 * delay;
//            }

//            return delay;
//        }

//        public static double GetPlaybackPosition(ref PlaybackState playbackState, in AudioState audioState)
//        {
//            return audioState.AudioSource?.BuffersQueued > 0
//                ? AudioRenderer.GetPlaybackPosition(audioState)
//                : playbackState.ExternalClock.Get();
//        }


//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void AdvanceClock(
//            ref PlaybackState playbackState, ref VideoState videoState,
//            in MediaFrame lastTakenFrame, double delay)
//        {
//            SetNextRefreshTime(ref playbackState, ref videoState);
//            videoState.VideoClock.Set(lastTakenFrame.PresentationTimestamp);
//            playbackState.ExternalClock.SyncTo(videoState.VideoClock);

//            void SetNextRefreshTime(ref PlaybackState ps, ref VideoState vs)
//            {
//                double time = GetTime(ref ps);
//                vs.NextRefreshTime += delay;
//                if (delay > 0 && (time - vs.NextRefreshTime) > SyncThresholdMax)
//                {
//                    vs.NextRefreshTime = time;
//                }
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static double GetTime(ref PlaybackState playbackState)
//            => playbackState.Stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
//    }
//}
