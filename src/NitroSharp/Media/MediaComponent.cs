using System;
using System.Diagnostics;
using NitroSharp.Graphics;
using Veldrid;
using NitroSharp.Media.Decoding;
using System.Collections.Generic;
using NitroSharp.Content;
using NitroSharp.Primitives;
using System.Numerics;

namespace NitroSharp.Media
{
    internal class MediaComponent : Visual
    {
        private const double SyncThresholdMin = 0.04d;
        private const double SyncThresholdMax = 0.1d;

        private readonly AssetRef<MediaPlaybackSession> _sessionRef;
        private readonly MediaPlaybackSession _playbackSession;
        private readonly Stopwatch _stopwatch;
        private Clock _videoClock, _externalClock;

        private BindableTexture _videoTexture;
        private Texture _stagingTexture;
        private CommandList _cl;

        private readonly AudioSourcePool _audioSourcePool;
        private readonly AudioSource _audioSource;
        private readonly Dictionary<IntPtr, MediaFrame> _submittedAudioFrames;

        // The time when the next frame should be displayed, in seconds.
        private double _nextRefreshTime;

        private (TimeSpan loopStart, TimeSpan loopEnd)? _loopRegion;
        private bool _seeking;
        private double _seekTarget;

        public MediaComponent(AssetRef<MediaPlaybackSession> session, AudioSourcePool audioSourcePool)
        {
            _sessionRef = session;
            _audioSourcePool = audioSourcePool;
            _playbackSession = session.Asset;

            _stopwatch = new Stopwatch();
            _videoClock = new Clock(_stopwatch);
            _externalClock = new Clock(_stopwatch);

            if (_playbackSession.Container.HasAudio)
            {
                Duration = _playbackSession.AudioStream.Duration;
                _submittedAudioFrames = new Dictionary<IntPtr, MediaFrame>();
                _audioSource = _audioSourcePool.Rent();
            }

            if (_playbackSession.VideoStream != null)
            {
                var video = _playbackSession.VideoStream;
                Bounds = new SizeF(video.Width, video.Height);
            }

            Volume = 1;
            Priority = 100;
        }

        public override SizeF Bounds { get; }

        public TimeSpan Duration { get; }
        public TimeSpan Elapsed => TimeSpan.FromSeconds(GetPlaybackPosition());
        public TimeSpan PlaybackPosition => TimeSpan.FromSeconds(GetPlaybackPosition());

        public double SoundAmplitude { get; private set; }
        public bool EnableLooping { get; internal set; }

        public float Volume
        {
            get => _audioSource != null ? _audioSource.Volume : 0;
            set
            {
                if (_audioSource != null)
                {
                    _audioSource.Volume = value;
                }
            }
        }

        public void SetLoopRegion(TimeSpan loopStart, TimeSpan loopEnd)
        {
            _loopRegion = (loopStart, loopEnd);
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            if (!_playbackSession.Container.HasVideo)
            {
                _playbackSession.Seek(0);
            }
            if (!_playbackSession.IsRunning)
            {
                _playbackSession.Start();
            }

            if (_playbackSession.Container.HasVideo)
            {
                var texturePool = renderContext.TexturePool;
                var size = new Size((uint)Bounds.Width, (uint)Bounds.Height);
                Texture sampled = texturePool.RentSampled(size);
                _videoTexture = new BindableTexture(renderContext.Factory, sampled);
                _stagingTexture = texturePool.RentStaging(size);
                _cl = renderContext.Factory.CreateCommandList();
            }
            if (_playbackSession.Container.HasAudio)
            {
                _audioSource.Play();
            }

            _stopwatch.Start();
        }

        public override void DestroyDeviceObjects(RenderContext renderContext)
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.FlushBuffers();

                bool gotBuffer = false;
                IntPtr bufferPtr = default;
                while (_audioSource.BuffersQueued > 0 || (gotBuffer = _audioSource.TryDequeueProcessedBuffer(out bufferPtr)))
                {
                    if (gotBuffer)
                    {
                        MediaFrame frame = _submittedAudioFrames[bufferPtr];
                        frame.Free();
                    }
                }

                _submittedAudioFrames.Clear();
                _audioSourcePool.Return(_audioSource);
            }

            _sessionRef.Dispose();

            if (_videoTexture != null)
            {
                renderContext.TexturePool.Return(_videoTexture);
                renderContext.TexturePool.Return(_stagingTexture);
            }

            _stopwatch.Stop();
        }

        public override void Render(RenderContext renderContext)
        {
            if (!_playbackSession.IsRunning)
            {
                return;
            }

            if (_playbackSession.Container.HasVideo)
            {
                UpdateVideo(renderContext);
            }
            if (_playbackSession.Container.HasAudio)
            {
                UpdateAudio();
            }

            DisplayCurrentFrame(renderContext);
        }

        private void UpdateVideo(RenderContext renderContext)
        {
            MediaFrameQueue<MediaFrame> frameQueue = _playbackSession.VideoFrameQueue;
            if (frameQueue.TryPeek(out MediaFrame frame))
            {
                if (ShouldSkip(ref frame))
                {
                    frameQueue.Take();
                    frame.Free();
                    return;
                }

                double delay = CalculateDelay(frame.PresentationTimestamp, _videoClock.LastPresentationTimestamp);
                if (ShouldTakeFrame(delay))
                {
                    frameQueue.Take();
                    if (!ShouldSkip(ref frame))
                    {
                        AdvanceClock(frame, delay);
                        UpdateTexture(renderContext, frame.Buffer);
                        frame.Free();
                    }
                    else
                    {
                        frame.Free();
                    }
                }
            }
        }

        private void UpdateAudio()
        {
            while (_audioSource.TryDequeueProcessedBuffer(out IntPtr pointer))
            {
                MediaFrame frame = _submittedAudioFrames[pointer];
                frame.Free();
            }

            SoundAmplitude = CalculateAmplitude(_audioSource.CurrentBuffer);

            MediaFrameQueue<MediaFrame> bufferQueue = _playbackSession.AudioBufferQueue;
            int count = 0;
            while (bufferQueue.TryPeek(out MediaFrame audio))
            {
                count++;
                if (ShouldSkip(ref audio))
                {
                    bufferQueue.Take();
                    audio.Free();
                    continue;
                }

                if (_audioSource.TrySubmitBuffer(audio.Buffer.Data, audio.Buffer.Size))
                {
                    _submittedAudioFrames[audio.Buffer.Data] = audio;
                    bufferQueue.Take();
                }
                else
                {
                    break;
                }
            }
        }

        private void Seek(double timestamp)
        {
            _playbackSession.Seek(timestamp);
            _seekTarget = timestamp;
            _seeking = true;
        }

        private bool ShouldSkip(ref MediaFrame frame)
        {
            if (_seeking)
            {
                bool reachedTargetTime = frame.ContainsTimestamp(_seekTarget);
                if (reachedTargetTime)
                {
                    _seeking = false;
                }

                return !reachedTargetTime;
            }

            if (EnableLooping)
            {
                if (_loopRegion.HasValue)
                {
                    (TimeSpan loopStart, TimeSpan loopEnd) = _loopRegion.Value;
                    if (frame.ContainsTimestamp(loopEnd.TotalSeconds))
                    {
                        Seek(loopStart.TotalSeconds);
                    }
                }
                else
                {
                    if (frame.IsEofFrame)
                    {
                        Seek(0);
                    }
                }
            }

            return frame.IsEofFrame;
        }

        private double CalculateAmplitude(IntPtr sampleBuffer)
        {
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

        /// <summary>
        /// Returns true if it's time to take the next frame out of the queue and display it.
        /// </summary>
        private bool ShouldTakeFrame(double delay)
        {
            double diff = GetTime() - (_nextRefreshTime + delay);
            return diff >= 0;
        }

        /// <summary>
        /// Calculates the delay between two frames using their PTS values.
        /// </summary>
        private double CalculateDelay(double pts, double prevPts)
        {
            double delay = pts - prevPts;
            double diff = _videoClock.Get() - GetPlaybackPosition();
            double syncThreshold = Math.Max(SyncThresholdMin, Math.Min(SyncThresholdMax, diff));

            if (diff <= -syncThreshold) // the video is behind
            {
                delay = Math.Max(0, delay + diff);
            }
            else if (diff >= syncThreshold && delay > 0.1d)
            {
                delay = delay + diff;
            }
            else if (diff >= syncThreshold)
            {
                delay = 2 * delay;
            }

            return delay;
        }

        private void AdvanceClock(in MediaFrame lastTakenFrame, double delay)
        {
            SetNextRefreshTime();
            _videoClock.Set(lastTakenFrame.PresentationTimestamp);
            _externalClock.SyncTo(_videoClock);

            void SetNextRefreshTime()
            {
                double time = GetTime();
                _nextRefreshTime += delay;
                if (delay > 0 && (time - _nextRefreshTime) > SyncThresholdMax)
                {
                    _nextRefreshTime = time;
                }
            }
        }

        private void DisplayCurrentFrame(RenderContext rc)
        {
            if (_videoTexture != null)
            {
                var rect = new RectangleF(Vector2.Zero, Bounds);
                rc.PrimitiveBatch.DrawImage(_videoTexture.GetTextureView(), null, rect, ref _color, BlendMode.Additive);
            }
        }

        private void UpdateTexture(RenderContext rc, in PooledBuffer buffer)
        {
            rc.Device.UpdateTexture(
                _stagingTexture, buffer.Data, buffer.Size, 0, 0, 0,
                (uint)Bounds.Width, (uint)Bounds.Height, 1, 0, 0);

            var cl = _cl;
            cl.Begin();
            cl.CopyTexture(_stagingTexture, _videoTexture);
            cl.End();
            rc.Device.SubmitCommands(cl);
        }

        private double GetPlaybackPosition()
        {
            return _audioSource?.BuffersQueued > 0
                ? GetAudioPosition()
                : _externalClock.Get();
        }

        private double GetAudioPosition()
        {
            IntPtr currentBuffer = _audioSource.CurrentBuffer;
            if (currentBuffer != IntPtr.Zero && _submittedAudioFrames.TryGetValue(currentBuffer, out MediaFrame frame))
            {
                double value = frame.PresentationTimestamp + _audioSource.PositionInCurrentBuffer;
                return value;
            }

            return 0;
        }

        private double GetTime() => _stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

        private struct Clock
        {
            private readonly Stopwatch _sw;
            private double _lastUpdated;

            public Clock(Stopwatch sw)
            {
                _sw = sw;
                LastPresentationTimestamp = 0;
                _lastUpdated = 0;
            }

            public double LastPresentationTimestamp { get; private set; }
            public double Drift => LastPresentationTimestamp - _lastUpdated;

            public void Set(double lastPresentationTimestamp)
            {
                LastPresentationTimestamp = lastPresentationTimestamp;
                _lastUpdated = GetTime();
            }

            public double Get()
            {
                double time = GetTime();
                return Drift + time - (time - _lastUpdated);
            }

            public void SyncTo(Clock other)
            {
                Set(other.Get());
            }

            private double GetTime() => _sw.ElapsedTicks / (double)Stopwatch.Frequency;
        }
    }
}
