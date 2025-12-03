using System;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics;

internal sealed class AnimatedIcons(Icon? waitLine) : IDisposable
{
    public Icon? WaitLine { get; } = waitLine;

    public void Dispose()
    {
        WaitLine?.Dispose();
    }
}

internal sealed class Icon : IDisposable
{
    private sealed class IconAnimation// : UIntAnimation<Icon>
    {
        // public IconAnimation(
        //     Icon icon,
        //     uint startValue, uint endValue,
        //     TimeSpan duration)
        //     : base(icon, startValue, endValue, duration, NsEaseFunction.Linear, repeat: true)
        // {
        // }
        //
        // protected override ref uint GetValueRef() => ref _entity._activeFrame;
    }

    private readonly Texture _texture;
    private readonly IconAnimation _animation;
    private uint _activeFrame;

    private Icon(Texture texture)
    {
        _texture = texture;
        uint frameCount = _texture.ArrayLayers;
        var duration = TimeSpan.FromMilliseconds(frameCount * 120);
        //_animation = new IconAnimation(this, 0, frameCount - 1, duration);
        _animation = new();
    }

    public static bool Exists(ContentManager content, IconPathPattern pathPattern)
    {
        foreach (string path in pathPattern.EnumeratePaths())
        {
            using Stream? stream = content.TryOpenStream(path);
            if (stream is null)
            {
                return false;
            }
        }

        return true;
    }

    public static Icon Load(RenderContext renderContext, IconPathPattern pathPattern)
    {
        ContentManager content = renderContext.Content;
        ResourceFactory rf = renderContext.ResourceFactory;
        Texture? texture = null;
        CommandList cl = renderContext.CommandListPool.Rent();
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
                source: staging,
                srcX: 0, srcY: 0, srcZ: 0,
                srcMipLevel: 0, srcBaseArrayLayer: 0,
                destination: texture,
                dstX: 0, dstY: 0, dstZ: 0,
                dstMipLevel: 0, layer++,
                texture.Width, texture.Height,
                depth: 1, layerCount: 1
            );
        }
        cl.End();
        renderContext.GraphicsDevice.SubmitCommands(cl);
        renderContext.CommandListPool.Return(cl);

        Debug.Assert(texture is not null);
        return new Icon(texture);
    }

    public void Reset()
    {
        // _animation.Reset();
    }

    public void Update(float dt)
    {
        // _animation.Update(dt);
    }

    public void Render(RenderContext context, Vector2 position)
    {
        DrawBatch batch = context.MainBatch;
        IconShaderResources shaderResources = context.ShaderResources.Icon;
        ViewProjection vp = context.OrthoProjection;

        var transform = Matrix4x4.CreateTranslation(new Vector3(position, 0));
        var quad = QuadPrimitive.Create(
            new DesignSize(_texture.Width, _texture.Height),
            transform,
            uvTopLeft: Vector2.Zero,
            uvBottomRight: Vector2.One,
            color: Vector4.One
        );

        var quadUV3 = QuadPrimitiveUV3.FromQuad(quad, layer: _activeFrame);
        PrimitiveSlice<QuadVertexUV3> slice = context.QuadsUV3.Append(quadUV3.AsSpan());

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
            BufferBindings = new BufferBindings(slice.Vertices.Buffer, slice.Indices.Buffer),
            Params = DrawParams.Indexed(0, 0, 6)
        });
    }

    public void Dispose()
    {
        _texture.Dispose();
    }
}
