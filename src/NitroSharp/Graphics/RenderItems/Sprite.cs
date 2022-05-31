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
        StandaloneTexture
    }

    internal readonly struct SpriteTexture : IDisposable
    {
        public readonly SpriteTextureKind Kind;
        public readonly AssetRef<Texture>? AssetRef;
        public readonly DesignRectU? SourceRectangle;
        public readonly Texture? Standalone;
        public readonly RgbaFloat Color;

        private SpriteTexture(
            SpriteTextureKind kind,
            AssetRef<Texture>? assetRef,
            DesignRectU? sourceRectangle,
            Texture? standalone,
            in RgbaFloat color)
        {
            Kind = kind;
            AssetRef = assetRef;
            SourceRectangle = sourceRectangle;
            Standalone = standalone;
            Color = color;
        }

        public static SpriteTexture FromSaveData(in SpriteTextureSaveData saveData, GameLoadingContext ctx)
        {
            switch (saveData.Kind)
            {
                case SpriteTextureKind.SolidColor:
                    return new SpriteTexture(
                        SpriteTextureKind.SolidColor,
                        assetRef: null,
                        saveData.SourceRectangle,
                        standalone: null,
                        new RgbaFloat(saveData.Color)
                    );
                case SpriteTextureKind.Asset:
                    Debug.Assert(saveData.AssetPath is not null);
                    // TODO: error handling?
                    AssetRef<Texture> assetRef = ctx.Content.RequestTexture(saveData.AssetPath)!.Value;
                    return FromAsset(assetRef, saveData.SourceRectangle);
                case SpriteTextureKind.StandaloneTexture:
                    Debug.Assert(saveData.StandaloneTextureId is not null);
                    int id = saveData.StandaloneTextureId.Value;
                    return FromStandalone(ctx.StandaloneTextures[id], ctx.Rendering);
                default:
                    return ThrowHelper.Unreachable<SpriteTexture>();
            }
        }

        public SpriteTextureSaveData ToSaveData(GameSavingContext ctx)
        {
            int? standaloneTextureId = this is { Kind: SpriteTextureKind.StandaloneTexture, Standalone: { } standalone }
                ? ctx.AddStandaloneTexture(standalone)
                : null;

            return new SpriteTextureSaveData
            {
                Kind = Kind,
                Color = Color.ToVector4(),
                AssetPath = AssetRef?.Path,
                SourceRectangle = SourceRectangle,
                StandaloneTextureId = standaloneTextureId
            };
        }

        public static SpriteTexture FromAsset(AssetRef<Texture> assetRef, DesignRectU? srcRectangle = null)
            => new(SpriteTextureKind.Asset, assetRef, srcRectangle, null, RgbaFloat.White);

        public static SpriteTexture SolidColor(in RgbaFloat color, DesignSizeU size) => new(
            SpriteTextureKind.SolidColor,
            null,
            new DesignRectU(0, 0, size.Width, size.Height),
            null,
            color
        );

        public static SpriteTexture FromStandalone(Texture texture, RenderContext ctx) => new(
            SpriteTextureKind.StandaloneTexture,
            null,
            new DesignRectU(DesignPointU.Zero, texture.Size().Convert(ctx.DeviceToWorldScale)),
            texture,
            RgbaFloat.White
        );

        public Texture Resolve(RenderContext ctx) => this switch
        {
            { Kind: SpriteTextureKind.Asset, AssetRef: { } assetRef } => ctx.Content.Get(assetRef),
            { Kind: SpriteTextureKind.SolidColor } => ctx.WhiteTexture,
            { Kind: SpriteTextureKind.StandaloneTexture, Standalone: { } standalone } => standalone,
            _ => ThrowHelper.Unreachable<Texture>()
        };

        public DesignSizeU GetSize(RenderContext ctx) => this switch
        {
            { SourceRectangle: { } srcRect } => srcRect.Size,
            { AssetRef: { } assetRef } => ctx.Content.GetTextureSize(assetRef).Convert(Scale<ScreenPixel, DesignPixel>.Identity),
            _ => ThrowHelper.Unreachable<DesignSizeU>()
        };

        public (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        {
            if (this is { AssetRef: { } assetRef, SourceRectangle: { } srcRect })
            {
                var texSize = ctx.Content.GetTextureSize(assetRef).ToVector2();
                uint p = texSize.X <= 32 && texSize.Y <= 42 ? 0u : 1u;
                //var tl = new Vector2(srcRect.Left, srcRect.Top) / texSize;
                //var br = new Vector2(srcRect.Right, srcRect.Bottom) / texSize;
                var tl = new Vector2(srcRect.Left, srcRect.Top + p) / texSize;
                var br = new Vector2(srcRect.Right, srcRect.Bottom - p) / texSize;
                return (tl, br);
            }

            return (Vector2.Zero, Vector2.One);
        }

        public SpriteTexture WithSourceRectangle(DesignRectU? sourceRect)
        {
            return this is { Kind: SpriteTextureKind.Asset, AssetRef: { } assetRef }
                ? FromAsset(assetRef.Clone(), sourceRect)
                : new SpriteTexture(Kind, AssetRef, sourceRect ?? SourceRectangle, Standalone, Color);
        }

        public SpriteTexture Clone()
        {
            return this is { Kind: SpriteTextureKind.Asset, AssetRef: { } assetRef }
                ? FromAsset(assetRef.Clone(), SourceRectangle)
                : this;
        }

        public void Dispose()
        {
            if (this is { Kind: SpriteTextureKind.Asset, AssetRef: { } assetRef })
            {
                assetRef.Dispose();
            }
            if (this is { Kind: SpriteTextureKind.StandaloneTexture, Standalone: { } tex})
            {
                tex.Dispose();
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
            return Parent is Choice
                && _texture.Kind == SpriteTextureKind.Asset
                && _texture.SourceRectangle is null;
        }

        public override DesignSize GetUnconstrainedBounds(RenderContext ctx)
            => _texture.GetSize(ctx).ToSizeF();

        public override bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Transition => _transition is not null,
            _ => base.IsAnimationActive(kind)
        };

        protected override (Vector2, Vector2) GetTexCoords(RenderContext ctx)
            => _texture.GetTexCoords(ctx);

        public override void Render(RenderContext ctx, bool assetsReady)
        {
            if (assetsReady && _transition is { } transition)
            {
                Texture src = RenderOffscreen(ctx);
                Texture mask = ctx.Content.Get(transition.Mask);
                RenderTransition(ctx, ctx.MainBatch, src, mask, transition.FadeAmount);
                if (_transition.HasCompleted)
                {
                    _transition.Dispose();
                    _transition = null;
                }
            }
            else
            {
                base.Render(ctx, assetsReady);
            }
        }

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            Texture alphaMaskTex = ctx.WhiteTexture;
            Vector2 alphaMaskPos = Vector2.Zero;
            if (Parent is AlphaMask alphaMask)
            {
                alphaMaskTex = ctx.Content.Get(alphaMask.Texture);
                //alphaMaskPos = alphaMask.Transform.Position.XY();
                alphaMaskPos = alphaMask.DeviceBoundingRect.Position;
            }

            drawBatch.PushQuad(
                Quad,
                _texture.Resolve(ctx),
                alphaMaskTex,
                alphaMaskPos,
                BlendMode,
                ctx.GetSampler(FilterMode, DeviceBoundingRect.Size)
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
        public DesignRectU? SourceRectangle { get; init; }
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
                    SourceRectangle = new DesignRectU(DesignPointU.Zero, new DesignSizeU(ref reader));
                    break;
                case SpriteTextureKind.StandaloneTexture:
                    StandaloneTextureId = reader.ReadNullableInt32();
                    break;
                case SpriteTextureKind.Asset:
                    AssetPath = reader.ReadString();
                    if (!reader.TryReadNil())
                    {
                        SourceRectangle = new DesignRectU(ref reader);
                    }
                    break;
            }
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            int fieldCount = Kind switch
            {
                SpriteTextureKind.SolidColor => 3,
                SpriteTextureKind.StandaloneTexture => 2,
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
                case SpriteTextureKind.StandaloneTexture:
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
