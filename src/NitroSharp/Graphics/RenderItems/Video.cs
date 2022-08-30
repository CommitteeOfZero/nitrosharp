using System.IO;
using System.Numerics;
using NitroSharp.Graphics.Core;
using NitroSharp.Media;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal class Video : RenderItem2D
    {
        private readonly bool _enableAlpha;
        private readonly PooledAudioSource _audioSource;
        private bool _playbackStarted;

        public Video(
            in ResolvedEntityPath path,
            int priority,
            RenderContext renderContext,
            AudioContext audioContext,
            Stream stream,
            bool alpha = false)
            : base(path, priority)
        {
            _enableAlpha = alpha;
            _audioSource = audioContext.RentAudioSource();
            Stream = new MediaStream(
                stream,
                renderContext.GraphicsDevice,
                _audioSource.Value,
                audioContext.Device.AudioParameters
            );
            Color = RgbaFloat.White;
        }

        public Video(in ResolvedEntityPath path, in RenderItemSaveData saveData)
            : base(in path, in saveData)
        {
        }

        public override EntityKind Kind => EntityKind.Video;

        public MediaStream Stream { get; }

        public override bool IsIdle => !Stream.IsPlaying;

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            Size res = Stream.VideoResolution;
            if (_enableAlpha)
            {
                res = new Size(res.Width, res.Height / 2);
            }

            return res;
        }

        protected override void RenderCore(RenderContext ctx, DrawBatch drawBatch)
        {
            if (!Stream.IsPlaying)
            {
                return;
            }

            GraphicsDevice gd = ctx.GraphicsDevice;
            if (Stream.GetNextFrame(out YCbCrFrame frame))
            {
                _playbackStarted = true;
                using (frame)
                {
                    CommandList cl = ctx.CommandListPool.Rent();
                    cl.Begin();
                    frame.CopyToDeviceMemory(cl);
                    cl.End();
                    gd.SubmitCommands(cl);
                    ctx.CommandListPool.Return(cl);
                }
            }

            if (!_playbackStarted) { return; }

            (Texture luma, Texture chroma) = Stream.VideoFrames.GetDeviceTextures();
            VideoShaderResources shaderResources = ctx.ShaderResources.Video;
            ViewProjection vp = drawBatch.Target.OrthoProjection;

            Vector4 enableAlpha = _enableAlpha ? Vector4.One : Vector4.Zero;
            drawBatch.UpdateBuffer(shaderResources.EnableAlphaBuffer, enableAlpha);
            drawBatch.PushQuad(Quad,
                shaderResources.GetPipeline(BlendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        shaderResources.InputLayout,
                        luma,
                        chroma,
                        ctx.GetSampler(FilterMode.Linear)
                    ),
                    new ResourceSetKey(
                        shaderResources.ParamLayout,
                        shaderResources.EnableAlphaBuffer.VdBuffer
                    )
                )
            );
        }

        public override void Dispose()
        {
            Stream.Dispose();
            _audioSource.Dispose();
        }
    }
}
