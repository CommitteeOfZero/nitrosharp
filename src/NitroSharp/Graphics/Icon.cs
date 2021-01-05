using System;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.NsScript;
using Veldrid;

#nullable enable

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

    internal struct IconVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public float Layer;
        private Vector3 _padding;

        public static readonly VertexLayoutDescription LayoutDescription = new(
            stride: 32,
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_TexCoord",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_Layer",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float1
            ),
            new VertexElementDescription(
                "vs_Padding",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
            )
        );
    }

    internal struct IconGeometry
    {
        public IconVertex TopLeft;
        public IconVertex TopRight;
        public IconVertex BottomLeft;
        public IconVertex BottomRight;

        public static IconGeometry FromQuad(in QuadGeometry quad, uint layer)
        {
            static IconVertex vertex(in QuadVertex v, uint layer)
            {
                return new()
                {
                    Position = v.Position,
                    TexCoord = v.TexCoord,
                    Layer = layer
                };
            }

            return new IconGeometry
            {
                TopLeft = vertex(quad.TopLeft, layer),
                TopRight = vertex(quad.TopRight, layer),
                BottomLeft = vertex(quad.BottomLeft, layer),
                BottomRight = vertex(quad.BottomRight, layer)
            };
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
            CommandList cl = renderContext.TransferCommands;
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

            Debug.Assert(texture is object);
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

            MeshList<IconVertex> icons = context.IconQuads;
            var geometry = IconGeometry.FromQuad(quad, layer: _activeFrame);
            Mesh<IconVertex> mesh = icons
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
