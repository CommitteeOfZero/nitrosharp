using System;
using System.Diagnostics;
using System.Numerics;
using MessagePack;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp.Graphics
{
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
        public RectangleU? SourceRectangle { get; private init; }

        public static SpriteTexture FromSaveData(in SpriteTextureSaveData saveData, GameLoadingContext ctx)
        {
            switch (saveData.Kind)
            {
                case SpriteTextureKind.SolidColor:
                    return new SpriteTexture(
                        SpriteTextureKind.SolidColor,
                        assetRef: null,
                        sourceRectangle: saveData.SourceRectangle,
                        texture: null,
                        pooledTexture: null,
                        color: new RgbaFloat(saveData.Color)
                    );
                case SpriteTextureKind.Asset:
                    Debug.Assert(saveData.AssetPath is not null);
                    // TODO: error handling?
                    AssetRef<Texture> assetRef = ctx.Content.RequestTexture(saveData.AssetPath)!.Value;
                    return FromAsset(assetRef, saveData.SourceRectangle);
                case SpriteTextureKind.Owned:
                    Debug.Assert(saveData.StandaloneTextureId is not null);
                    int id = saveData.StandaloneTextureId.Value;
                    return FromOwnedTexture(ctx.StandaloneTextures[id]);
                default:
                    return ThrowHelper.Unreachable<SpriteTexture>();
            }
        }

        private SpriteTexture(
            SpriteTextureKind kind,
            AssetRef<Texture>? assetRef,
            RectangleU? sourceRectangle,
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

        public SpriteTextureSaveData ToSaveData(GameSavingContext ctx)
        {
            Texture? texture = this switch
            {
                { Kind: SpriteTextureKind.Pooled, _pooledTexture: not null } => _pooledTexture.Value.Get(),
                { Kind: SpriteTextureKind.Owned or SpriteTextureKind.Pooled } => _texture,
                _ => null
            };

            int? standaloneTextureId = texture is not null
                ? ctx.AddStandaloneTexture(texture)
                : null;

            SpriteTextureKind newKind = Kind is SpriteTextureKind.Pooled or SpriteTextureKind.Borrowed
                ? SpriteTextureKind.Owned
                : Kind;

            return new SpriteTextureSaveData
            {
                Kind = newKind,
                Color = Color.ToVector4(),
                AssetPath = _assetRef?.Path,
                SourceRectangle = SourceRectangle,
                StandaloneTextureId = standaloneTextureId
            };
        }

        public static SpriteTexture FromAsset(AssetRef<Texture> assetRef, RectangleU? srcRectangle = null)
            => new(SpriteTextureKind.Asset, assetRef, srcRectangle, null, null, RgbaFloat.White);

        public static SpriteTexture SolidColor(in RgbaFloat color, Size size) => new(
            SpriteTextureKind.SolidColor,
            null,
            new RectangleU(0, 0, size.Width, size.Height), null,
            null,
            color
        );

        public static SpriteTexture FromPooledTexture(PooledTexture texture) => new(
            SpriteTextureKind.Pooled,
            null,
            new RectangleU(0, 0, texture.Get().Width, texture.Get().Height),
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
            new RectangleU(0, 0, texture.Width, texture.Height),
            texture,
            pooledTexture: null,
            color: RgbaFloat.White
        );

        public Texture Resolve(RenderContext ctx) => this switch
        {
            { Kind: SpriteTextureKind.Asset, _assetRef: { } assetRef } => ctx.Content.Get(assetRef),
            { Kind: SpriteTextureKind.SolidColor } => ctx.WhiteTexture,
            { Kind: SpriteTextureKind.Owned or SpriteTextureKind.Borrowed, _texture: not null } => _texture,
            { Kind: SpriteTextureKind.Pooled, _pooledTexture: { } pooledTexture } => pooledTexture.Get(),
            _ => ThrowHelper.Unreachable<Texture>()
        };

        public Size GetSize(RenderContext ctx) => this switch
        {
            { SourceRectangle: { } srcRect } => srcRect.Size,
            { _assetRef: { } assetRef } => ctx.Content.GetTextureSize(assetRef),
            _ => ThrowHelper.Unreachable<Size>()
        };

        public (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        {
            if (this is { _assetRef: { } assetRef, SourceRectangle: { } srcRect })
            {
                var texSize = ctx.Content.GetTextureSize(assetRef).ToVector2();
                var tl = new Vector2(srcRect.Left, srcRect.Top) / texSize;
                var br = new Vector2(srcRect.Right, srcRect.Bottom) / texSize;
                return (tl, br);
            }

            return (Vector2.Zero, Vector2.One);
        }

        public SpriteTexture WithSourceRectangle(RectangleU? sourceRect)
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

    internal class Sprite : RenderItem2D
    {
        private readonly SpriteTexture _texture;
        private TransitionAnimation? _transition;

        public Sprite(
            in ResolvedEntityPath path,
            int priority,
            in SpriteTexture texture)
            : base(path, priority)
        {
            _texture = texture;
            Color = texture.Color;
            PreciseHitTest = NeedPreciseHitTest();
        }

        public Sprite(in ResolvedEntityPath path, in SpriteSaveData saveData, GameLoadingContext ctx)
            : base(path, saveData.RenderItemData)
        {
            _texture = SpriteTexture.FromSaveData(saveData.Texture, ctx);
            PreciseHitTest = NeedPreciseHitTest();
            if (saveData.TransitionData is { } transitionData)
            {
                _transition = new TransitionAnimation(transitionData, ctx.Content);
            }
        }

        public override EntityKind Kind => EntityKind.Sprite;

        protected override bool PreciseHitTest { get; }
        public override bool IsIdle => base.IsIdle && _transition is null;

        private bool NeedPreciseHitTest()
        {
            return Parent.IsMouseStateEntity()
                && _texture.Kind == SpriteTextureKind.Asset
                && _texture.SourceRectangle is null;
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => _texture.GetSize(ctx);

        public override bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Transition => _transition is not null,
            _ => base.IsAnimationActive(kind)
        };

        protected override (Vector2, Vector2) GetTexCoords(RenderContext ctx)
            => _texture.GetTexCoords(ctx);

        public override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            if (_transition is { } transition)
            {
                Texture? mask = ctx.Content.TryGet(transition.Mask);
                if (mask is null) { return; }

                Texture src = RenderOffscreen(ctx);
                RenderTransition(ctx, drawBatch, src, mask, transition.FadeAmount);
                if (_transition.HasCompleted)
                {
                    _transition.Dispose();
                    _transition = null;
                }
            }
            else
            {
                base.Render(ctx, drawBatch);
            }
        }

        protected override void RenderCore(RenderContext ctx, DrawBatch drawBatch)
        {
            Texture alphaMaskTex = ctx.WhiteTexture;
            Vector2 alphaMaskPos = Vector2.Zero;
            if (TryGetAlphaMaskAscendant() is { } alphaMask)
            {
                alphaMaskTex = ctx.Content.Get(alphaMask.Texture);
                alphaMaskPos = alphaMask.Transform.Position.XY();
            }

            drawBatch.PushQuad(
                Quad,
                _texture.Resolve(ctx),
                alphaMaskTex,
                alphaMaskPos,
                BlendMode,
                FilterMode
            );
        }

        protected override void AdvanceAnimations(RenderContext ctx, float dt, bool assetsReady)
        {
            base.AdvanceAnimations(ctx, dt, assetsReady);
            if (assetsReady)
            {
                if (_transition?.Update(dt) == false)
                {
                    Color.SetAlpha(_transition.FadeAmount);
                }
            }
        }

        public void BeginTransition(
            AssetRef<Texture> mask,
            float srcFadeAmount, float dstFadeAmount,
            TimeSpan duration,
            NsEaseFunction easeFunction)
        {
            _transition = new TransitionAnimation(
                mask,
                srcFadeAmount, dstFadeAmount,
                duration,
                easeFunction
            );
        }

        private void RenderTransition(
            RenderContext ctx,
            DrawBatch drawBatch,
            Texture src, Texture mask,
            float fadeAmount)
        {
            ViewProjection vp = ctx.OrthoProjection;
            TransitionShaderResources resources = ctx.ShaderResources.Transition;
            drawBatch.UpdateBuffer(resources.ProgressBuffer, new Vector4(fadeAmount, 0, 0, 0));
            drawBatch.PushQuad(
                Quad,
                resources.Pipeline,
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        resources.InputLayout,
                        src,
                        mask,
                        ctx.GetSampler(FilterMode)
                    ),
                    new ResourceSetKey(
                        resources.ParamLayout,
                        resources.ProgressBuffer.VdBuffer
                    )
                )
            );
        }

        public new SpriteSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            RenderItemData = base.ToSaveData(ctx),
            Texture = _texture.ToSaveData(ctx),
            TransitionData = _transition?.ToSaveData()
        };

        public override void Dispose()
        {
            base.Dispose();
            _texture.Dispose();
        }
    }

    [Persistable]
    internal readonly partial struct SpriteSaveData : IEntitySaveData
    {
        public RenderItemSaveData RenderItemData { get; init; }
        public SpriteTextureSaveData Texture { get; init; }
        public TransitionAnimationSaveData? TransitionData { get; init; }

        public EntitySaveData CommonEntityData => RenderItemData.EntityData;
    }

    internal readonly struct SpriteTextureSaveData
    {
        public SpriteTextureKind Kind { get; init; }
        public Vector4 Color { get; init; }
        public string? AssetPath { get; init; }
        public RectangleU? SourceRectangle { get; init; }
        public int? StandaloneTextureId { get; init; }

        public SpriteTextureSaveData(ref MessagePackReader reader)
        {
            reader.ReadArrayHeader();
            Kind = (SpriteTextureKind)reader.ReadInt32();
            Color = Vector4.One;
            AssetPath = null;
            SourceRectangle = null;
            StandaloneTextureId = null;
            switch (Kind)
            {
                case SpriteTextureKind.SolidColor:
                    Color = reader.ReadVector4();
                    SourceRectangle = new RectangleU(Point2DU.Zero, new Size(ref reader));
                    break;
                case SpriteTextureKind.Owned:
                    StandaloneTextureId = reader.ReadNullableInt32();
                    break;
                case SpriteTextureKind.Asset:
                    AssetPath = reader.ReadString();
                    if (!reader.TryReadNil())
                    {
                        SourceRectangle = new RectangleU(ref reader);
                    }
                    break;
                case SpriteTextureKind.Borrowed or SpriteTextureKind.Pooled:
                    ThrowHelper.Unreachable();
                    break;
            }
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            int fieldCount = Kind switch
            {
                SpriteTextureKind.SolidColor => 3,
                SpriteTextureKind.Owned => 2,
                SpriteTextureKind.Asset => 3,
                _ => ThrowHelper.Unreachable<int>()
            };

            writer.WriteArrayHeader(fieldCount);
            writer.Write((int)Kind);
            switch (Kind)
            {
                case SpriteTextureKind.SolidColor:
                    Debug.Assert(SourceRectangle is { });
                    writer.Write(Color);
                    SourceRectangle.Value.Size.Serialize(ref writer);
                    break;
                case SpriteTextureKind.Owned:
                    writer.Write(StandaloneTextureId);
                    break;
                case SpriteTextureKind.Asset:
                    writer.Write(AssetPath);
                    if (SourceRectangle is { } srcRect)
                    {
                        srcRect.Serialize(ref writer);
                    }
                    else
                    {
                        writer.WriteNil();
                    }
                    break;
            }
        }
    }
}
