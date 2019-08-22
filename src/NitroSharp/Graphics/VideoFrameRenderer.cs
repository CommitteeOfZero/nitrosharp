using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Media.Decoding;
using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Media
{
    internal sealed class VideoRenderer : IDisposable
    {
        private readonly World _world;
        private readonly VideoClipTable _videoClips;
        private readonly RenderContext _renderContext;
        private readonly ContentManager _content;
        private readonly CommandList _cl;

        public VideoRenderer(World world, RenderContext renderContext, ContentManager content)
        {
            _world = world;
            _videoClips = world.VideoClips;
            _renderContext = renderContext;
            _content = content;
            _cl = renderContext.ResourceFactory.CreateCommandList();
        }

        private void ProcessRemoved()
        {
            foreach (VideoState videoState in _videoClips.VideoState.RecycledComponents)
            {
                _renderContext.TexturePool.Return(videoState.VideoTexture);
                _renderContext.TexturePool.Return(videoState.StagingTexture);
            }
        }

        private void ProcessNew()
        {
            var added = _videoClips.NewEntities;
            foreach (Entity e in added)
            {
                AssetId asset = _videoClips.Asset.GetValue(e);
                MediaPlaybackSession session = _content.GetMediaClip(asset);
                ref PlaybackState playbackState = ref _videoClips.PlaybackState.Mutate(e);
                PlaybackState.Initialize(ref playbackState, session);

                ref VideoState videoState = ref _videoClips.VideoState.Mutate(e);
                videoState.FrameQueue = session.VideoFrameQueue;
                videoState.VideoClock = new Clock(playbackState.Stopwatch);
                TexturePool texturePool = _renderContext.TexturePool;
                VideoStream stream = videoState.VideoStream = session.VideoStream;
                var size = new Size(stream.Width, stream.Height);
                videoState.VideoTexture = texturePool.RentSampled(size);
                videoState.StagingTexture = texturePool.RentStaging(size);

                if (!session.IsRunning)
                {
                    session.Start();
                }
                playbackState.Stopwatch.Start();
            }
        }

        //public static void RenderVideo<T>(T videoClips, Span<VideoState> videoState, ReadOnlySpan<AudioState> audioState)
        //    where T : RenderItemTable, MediaClipTable
        //{
        //    if (videoClips.ColumnsUsed == 0) { return; }
        //    TransformProcessor.ProcessTransforms(_world, videoClips);

        //    Span<PlaybackState> playbackState = videoClips.PlaybackState.MutateAll();
        //    ReadOnlySpan<MediaClipLoopData> loopData = videoClips.LoopData.Enumerate();

        //    int count = videoClips.ColumnsUsed;
        //    Span<TimeSpan> elapsed = videoClips.Elapsed.MutateAll();
        //    for (int i = 0; i < count; i++)
        //    {
        //        ref VideoState vs = ref videoState[i];
        //        UpdateVideo(ref playbackState[i], ref vs, audioState[i], loopData[i], ref elapsed[i]);
        //    }

        //    ReadOnlySpan<SizeF> bounds = videoClips.Bounds.Enumerate();
        //    ReadOnlySpan<Matrix4x4> transform = videoClips.TransformMatrices.Enumerate();
        //    ReadOnlySpan<RgbaFloat> color = videoClips.Colors.Enumerate();
        //    ReadOnlySpan<RenderItemKey> renderPriority = videoClips.SortKeys.Enumerate();
        //    for (int i = 0; i < count; i++)
        //    {
        //        DisplayCurrentFrame(
        //            ref videoState[i], bounds[i], transform[i],
        //            color[i], renderPriority[i]);
        //    }
        //}

        public void ProcessVideoClips()
        {
            ProcessRemoved();
            ProcessNew();

            if (_videoClips.EntryCount == 0) { return; }
            TransformProcessor.ProcessTransforms(_world, _videoClips);

            Span<PlaybackState> playbackState = _videoClips.PlaybackState.MutateAll();
            ReadOnlySpan<MediaClipLoopData> loopData = _videoClips.LoopData.Enumerate();

            int count = _videoClips.EntryCount;
            Span<VideoState> videoState = _videoClips.VideoState.MutateAll();
            ReadOnlySpan<AudioState> audioState = _videoClips.AudioState.Enumerate();
            Span<TimeSpan> elapsed = _videoClips.Elapsed.MutateAll();
            for (int i = 0; i < count; i++)
            {
                ref VideoState vs = ref videoState[i];
                UpdateVideo(ref playbackState[i], ref vs, audioState[i], loopData[i], ref elapsed[i]);
            }

            ReadOnlySpan<SizeF> bounds = _videoClips.Bounds.Enumerate();
            ReadOnlySpan<Matrix4x4> transform = _videoClips.TransformMatrices.Enumerate();
            ReadOnlySpan<RgbaFloat> color = _videoClips.Colors.Enumerate();
            ReadOnlySpan<RenderItemKey> renderPriority = _videoClips.SortKeys.Enumerate();
            for (int i = 0; i < count; i++)
            {
                DisplayCurrentFrame(
                    ref videoState[i], bounds[i], transform[i],
                    color[i], renderPriority[i]);
            }
        }

        private void UpdateVideo(
            ref PlaybackState playbackState,
            ref VideoState videoState,
            in AudioState audioState,
            in MediaClipLoopData loopData,
            ref TimeSpan elapsed)
        {
            MediaFrameQueue<MediaFrame> frameQueue = videoState.FrameQueue;
            if (frameQueue.TryPeek(out MediaFrame frame))
            {
                if (Synchonization.ShouldSkipFrame(ref frame, ref playbackState, loopData))
                {
                    frameQueue.Take();
                    frame.Free();
                    return;
                }

                double delay = Synchonization.CalculateDelay(
                    ref playbackState, ref videoState, audioState,
                    frame.PresentationTimestamp,
                    videoState.VideoClock.LastPresentationTimestamp);

                if (Synchonization.ShouldTakeFrame(ref playbackState, ref videoState, delay))
                {
                    frameQueue.Take();
                    if (!Synchonization.ShouldSkipFrame(ref frame, ref playbackState, loopData))
                    {
                        Synchonization.AdvanceClock(ref playbackState, ref videoState, frame, delay);
                        UpdateTexture(ref videoState, frame.Buffer);
                        frame.Free();
                    }
                    else
                    {
                        frame.Free();
                    }
                }
            }

            if (!playbackState.HasAudio)
            {
                elapsed = TimeSpan.FromSeconds(Synchonization.GetPlaybackPosition(ref playbackState, audioState));
            }
        }

        private void DisplayCurrentFrame(
            ref VideoState videoState, SizeF bounds,
            in Matrix4x4 transform, RgbaFloat color, RenderItemKey priority)
        {
            var rect = new RectangleF(Vector2.Zero, bounds);
            QuadBatcher batcher = _renderContext.QuadBatcher;
            batcher.SetTransform(transform);
            batcher.DrawImage(videoState.VideoTexture, rect, rect, ref color, priority, BlendMode.Additive);
        }

        private void UpdateTexture(ref VideoState videoState, in PooledBuffer buffer)
        {
            Texture staging = videoState.StagingTexture;
            _renderContext.Device.UpdateTexture(
                staging, buffer.Data, buffer.Size, 0, 0, 0,
                staging.Width, staging.Height, 1, 0, 0);

            CommandList cl = _cl;
            cl.Begin();
            cl.CopyTexture(staging, videoState.VideoTexture);
            cl.End();
            _renderContext.Device.SubmitCommands(cl);
        }

        public void Dispose()
        {
            ReadOnlySpan<VideoState> videoState = _videoClips.VideoState.Enumerate();
            for (int i = 0; i < videoState.Length; i++)
            {
            }

            _cl.Dispose();
        }
    }
}
