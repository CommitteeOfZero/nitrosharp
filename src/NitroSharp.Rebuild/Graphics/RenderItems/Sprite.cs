using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using Veldrid;

namespace NitroSharp.Graphics;

internal class Sprite : RenderItem
{
    private readonly SpriteTexture _texture;

    public Sprite(EntityName name, Entity? parent, int priority, in SpriteTexture texture) : base(name, parent, priority)
    {
        _texture = texture;
    }

    public SpriteTexture Texture => _texture;

    public override DesignSize GetSize(RenderContext ctx)
        => _texture.GetSize(ctx);

    protected override (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        => _texture.GetTexCoords(ctx);

    public override void Render(GameContext ctx)
    {
        RenderCore(ctx.RenderContext, ctx.RenderContext.MainBatch);
    }

    private void RenderCore(RenderContext ctx, DrawBatch drawBatch)
    {
        Texture alphaMaskTex = ctx.WhiteTexture;
        Vector2 alphaMaskPos = Vector2.Zero;
        // if (TryGetAlphaMaskAscendant() is { } alphaMask)
        // {
        //     alphaMaskTex = ctx.Content.Get(alphaMask.Texture);
        //     alphaMaskPos = alphaMask.Transform.Position.XY();
        // }

        drawBatch.PushQuad(
            Quad,
            _texture.Resolve(ctx),
            alphaMaskTex,
            alphaMaskPos,
            BlendMode,
            FilterMode
        );
    }
}

internal enum SpriteTextureKind
    {
        SolidColor,
        Asset,
        Owned,
        Borrowed,
        Pooled
    }

    internal readonly struct SpriteTexture : IDisposable
    {
        private readonly AssetRef<Texture>? _assetRef;
        private readonly Texture? _texture;
        private readonly PooledTexture? _pooledTexture;

        public readonly SpriteTextureKind Kind;
        public readonly RgbaFloat Color;
        public DesignRectU? SourceRectangle { get; private init; }

        private SpriteTexture(
            SpriteTextureKind kind,
            AssetRef<Texture>? assetRef,
            DesignRectU? sourceRectangle,
            Texture? texture,
            PooledTexture? pooledTexture,
            in RgbaFloat color)
        {
            Kind = kind;
            _assetRef = assetRef;
            SourceRectangle = sourceRectangle;
            _texture = texture;
            _pooledTexture = pooledTexture;
            Color = color;
        }

        public static SpriteTexture FromAsset(AssetRef<Texture> assetRef, DesignRectU? srcRectangle = null)
            => new(SpriteTextureKind.Asset, assetRef, srcRectangle, null, null, RgbaFloat.White);

        public static SpriteTexture SolidColor(in RgbaFloat color, DesignSizeU size) => new(
            SpriteTextureKind.SolidColor,
            null,
            new DesignRectU(0, 0, size.Width, size.Height), null,
            null,
            color
        );

        public static SpriteTexture FromPooledTexture(PooledTexture texture) => new(
            SpriteTextureKind.Pooled,
            null,
            new DesignRectU(0, 0, texture.Get().Width, texture.Get().Height),
            texture: null,
            pooledTexture: texture,
            color: RgbaFloat.White
        );

        public static SpriteTexture FromOwnedTexture(Texture texture)
            => FromTexture(SpriteTextureKind.Owned, texture);

        public static SpriteTexture FromBorrowedTexture(Texture texture)
            => FromTexture(SpriteTextureKind.Borrowed, texture);

        private static SpriteTexture FromTexture(SpriteTextureKind kind, Texture texture) => new(
            kind,
            null,
            new DesignRectU(0, 0, texture.Width, texture.Height),
            texture,
            pooledTexture: null,
            color: RgbaFloat.White
        );

        public Texture Resolve(RenderContext ctx)
            => this switch
            {
                { Kind: SpriteTextureKind.Asset, _assetRef: { } assetRef } => ctx.Content.Get(assetRef),
                { Kind: SpriteTextureKind.SolidColor } => ctx.WhiteTexture,
                { Kind: SpriteTextureKind.Owned or SpriteTextureKind.Borrowed, _texture: not null } => _texture,
                { Kind: SpriteTextureKind.Pooled, _pooledTexture: { } pooledTexture } => pooledTexture.Get(),
                _ => ThrowHelper.Unreachable<Texture>()
            };

        public DesignSize GetSize(RenderContext ctx)
            => this switch
            {
                { SourceRectangle: { } srcRect } => new DesignSize(srcRect.Width, srcRect.Height),
                { _assetRef: { } assetRef } => ctx.Content.GetTextureSize(assetRef).Reinterpret<DesignPixel>().ToFloatSize(),
                _ => ThrowHelper.Unreachable<DesignSize>()
            };

        public (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        {
            if (this is { _assetRef: { } assetRef, SourceRectangle: { } srcRect })
            {
                const float adjustment = 0.2f;
                var texSize = ctx.Content.GetTextureSize(assetRef).ToVector2();
                var topLeft = new Vector2(srcRect.Left + adjustment, srcRect.Top + adjustment) / texSize;
                var bottomRight = new Vector2(srcRect.Right - adjustment, srcRect.Bottom - adjustment) / texSize;
                return (topLeft, bottomRight);
            }

            return (Vector2.Zero, Vector2.One);
        }

        public SpriteTexture WithSourceRectangle(DesignRectU? sourceRect)
            => Clone() with { SourceRectangle = sourceRect ?? Clone().SourceRectangle };

        private SpriteTexture Clone()
            => this switch
            {
                { _assetRef: { } assetRef } => FromAsset(assetRef.Clone(), SourceRectangle),
                { _pooledTexture: { } pooledTexture } => FromBorrowedTexture(pooledTexture.Get()),
                _ => this
            };

        public void Dispose()
        {
            switch (this)
            {
                case { Kind: SpriteTextureKind.Asset, _assetRef: { } assetRef }:
                    assetRef.Dispose();
                    break;
                case { Kind: SpriteTextureKind.Pooled, _pooledTexture: { } pooledTexture}:
                    pooledTexture.Dispose();
                    break;
                case { Kind: SpriteTextureKind.Owned, _texture: { } standaloneTexture }:
                    standaloneTexture.Dispose();
                    break;
                case { Kind: SpriteTextureKind.Borrowed }:
                    break;
            }
        }
    }
