using System;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.NsScript;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class AnimatedIcons : IDisposable
    {
        public AnimatedIcons(Icon waitLine)
        {
            WaitLine = waitLine;
        }

        public Icon WaitLine { get; }

        public void Dispose()
        {
            WaitLine.Dispose();
        }
    }

    internal sealed class Icon : IDisposable
    {
        private sealed class IconAnimation : UIntAnimation<Icon>
        {
            public IconAnimation(
                Icon icon,
                uint startValue, uint endValue,
                TimeSpan duration)
                : base(icon, startValue, endValue, duration, NsEaseFunction.Linear, repeat: true)
            {
            }

            protected override ref uint GetValueRef() => ref _entity._activeFrame;
        }

        private readonly Texture _texture;
        private readonly IconAnimation _animation;
        private uint _activeFrame;

        private Icon(Texture texture)
        {
            _texture = texture;
            uint frameCount = _texture.ArrayLayers;
            var duration = TimeSpan.FromMilliseconds(frameCount * 120);
            _animation = new IconAnimation(this, 0, frameCount - 1, duration);
        }

        public static Icon Load(RenderContext renderContext, IconPathPattern pathPattern)
        {
            ContentManager content = renderContext.Content;
            ResourceFactory rf = renderContext.ResourceFactory;
            Texture? texture = null;
            CommandList cl = renderContext.RentCommandList();
            cl.Begin();
            uint layer = 0;
            foreach (string path in pathPattern.EnumeratePaths())
            {
                Texture staging = content.LoadTexture(path, staging: true);
                texture ??= rf.CreateTexture(TextureDescription.Texture2D(
                    staging.Width, staging.Height,
                    mipLevels: 1, arrayLayers: pathPattern.IconCount,
                    staging.Format, TextureUsage.Sampled
                ));
                cl.CopyTexture(
                    staging,
                    srcX: 0, srcY: 0, srcZ: 0,
                    srcMipLevel: 0, srcBaseArrayLayer: 0,
                    texture,
                    dstX: 0, dstY: 0, dstZ: 0,
                    dstMipLevel: 0, layer++,
                    texture.Width, texture.Height,
                    depth: 1, layerCount: 1
                );
            }
            cl.End();
            renderContext.GraphicsDevice.SubmitCommands(cl);
            renderContext.ReturnCommandList(cl);

            Debug.Assert(texture is not null);
            return new Icon(texture);
        }

        public void Reset()
        {
            _animation.Reset();
        }

        public void Update(float dt)
        {
            _animation.Update(dt);
        }

        public void Render(RenderContext context, Vector2 position)
        {
            DrawBatch batch = context.MainBatch;
            IconShaderResources shaderResources = context.ShaderResources.Icon;
            ViewProjection vp = context.OrthoProjection;

            (QuadGeometry quad, _) = QuadGeometry.Create(
                new SizeF(_texture.Width, _texture.Height),
                Matrix4x4.CreateTranslation(new Vector3(position, 0)),
                Vector2.Zero,
                Vector2.One,
                color: Vector4.One
            );

            var geometry = QuadGeometryUV3.FromQuad(quad, layer: _activeFrame);
            Mesh<QuadVertexUV3> mesh = context.QuadsUV3
                .Append(MemoryMarshal.CreateReadOnlySpan(ref geometry.TopLeft, 4));

            batch.PushDraw(new Draw
            {
                Pipeline = shaderResources.Pipeline,
                ResourceBindings = new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        shaderResources.ResourceLayout,
                        _texture,
                        context.GetSampler(FilterMode.Linear)
                    )
                ),
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, 0, 6)
            });
        }

        public void Dispose()
        {
            _texture.Dispose();
        }
    }
}
