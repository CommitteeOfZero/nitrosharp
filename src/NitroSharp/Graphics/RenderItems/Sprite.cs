using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript;
using Veldrid;

#nullable  enable

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
        public readonly RectangleU? SourceRectangle;
        public readonly Texture? Standalone;
        public readonly RgbaFloat Color;

        private SpriteTexture(
            SpriteTextureKind kind,
            AssetRef<Texture>? assetRef,
            RectangleU? sourceRectangle,
            Texture? standalone,
            in RgbaFloat color)
        {
            Kind = kind;
            AssetRef = assetRef;
            SourceRectangle = sourceRectangle;
            Standalone = standalone;
            Color = color;
        }

        public static SpriteTexture FromAsset(AssetRef<Texture> assetRef, RectangleU? srcRectangle = null)
            => new SpriteTexture(SpriteTextureKind.Asset, assetRef, srcRectangle, null, RgbaFloat.White);

        public static SpriteTexture SolidColor(in RgbaFloat color, Size size)
            => new SpriteTexture(SpriteTextureKind.SolidColor, null,
                new RectangleU(0, 0, size.Width, size.Height), null, color);

        public static SpriteTexture FromStandalone(Texture texture)
            => new SpriteTexture(SpriteTextureKind.StandaloneTexture, null,
                new RectangleU(0, 0, texture.Width, texture.Height), texture, RgbaFloat.White);

        public Texture Resolve(RenderContext ctx) => this switch
        {
            { Kind: SpriteTextureKind.Asset, AssetRef: { } assetRef } => ctx.Content.Get(assetRef),
            { Kind: SpriteTextureKind.SolidColor } => ctx.WhiteTexture,
            { Kind: SpriteTextureKind.StandaloneTexture, Standalone: { } standalone } => standalone,
            _ => ThrowHelper.Unreachable<Texture>()
        };

        public Size GetSize(RenderContext ctx) => this switch
        {
            { SourceRectangle: { } srcRect } => srcRect.Size,
            { AssetRef: { } assetRef } => ctx.Content.GetTextureSize(assetRef),
            _ => ThrowHelper.Unreachable<Size>()
        };

        public (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        {
            if (this is { AssetRef: { } assetRef, SourceRectangle: { } srcRect })
            {
                var texSize = ctx.Content.GetTextureSize(assetRef).ToVector2();
                var tl = new Vector2(srcRect.Left, srcRect.Top) / texSize;
                var br = new Vector2(srcRect.Right, srcRect.Bottom) / texSize;
                return (tl, br);
            }

            return (Vector2.Zero, Vector2.One);
        }

        public SpriteTexture WithSourceRectangle(RectangleU? sourceRect)
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
        }

        protected override bool PreciseHitTest => true;

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => _texture.GetSize(ctx);

        public override bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Transition => _transition is object,
            _ => base.IsAnimationActive(kind)
        };

        protected override (Vector2, Vector2) GetTexCoords(RenderContext ctx)
            => _texture.GetTexCoords(ctx);

        public override void Render(RenderContext ctx, bool assetsReady)
        {
            if (assetsReady && _transition is TransitionAnimation transition)
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

        protected override void AdvanceAnimations(float dt, bool assetsReady)
        {
            base.AdvanceAnimations(dt, assetsReady);
            if (assetsReady)
            {
                _transition?.Update(dt);
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
            ViewProjection vp = drawBatch.Target.ViewProjection;
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

        public override void Dispose()
        {
            base.Dispose();
            _texture.Dispose();
        }
    }
}
