using System;
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

        public Video(
            in ResolvedEntityPath path,
            int priority,
            RenderContext renderContext,
            AudioSourcePool audioSourcePool,
            Stream stream,
            bool alpha = false)
            : base(path, priority)
        {
            _enableAlpha = alpha;
            _audioSource = audioSourcePool.Rent();
            Stream = new MediaStream(
                stream,
                renderContext.GraphicsDevice,
                _audioSource.Value,
                audioSourcePool.AudioDevice.AudioParameters
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

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            if (!Stream.IsPlaying)
            {
                return;
            }

            RenderContext context = ctx;
            GraphicsDevice gd = ctx.GraphicsDevice;
            if (Stream.GetNextFrame(out YCbCrFrame frame))
            {
                using (frame)
                {
                    CommandList cl = ctx.RentCommandList();
                    cl.Begin();
                    frame.CopyToDeviceMemory(cl);
                    cl.End();
                    gd.SubmitCommands(cl);
                    ctx.ReturnCommandList(cl);
                }
            }

            (Texture luma, Texture chroma) = Stream.VideoFrames.GetDeviceTextures();
            VideoShaderResources shaderResources = context.ShaderResources.Video;
            ViewProjection vp = batch.Target.OrthoProjection;

            Vector4 enableAlpha = _enableAlpha ? Vector4.One : Vector4.Zero;
            batch.UpdateBuffer(shaderResources.EnableAlphaBuffer, enableAlpha);
            batch.PushQuad(Quad,
                shaderResources.GetPipeline(BlendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        shaderResources.InputLayout,
                        luma,
                        chroma,
                        context.GetSampler(FilterMode.Linear)
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
