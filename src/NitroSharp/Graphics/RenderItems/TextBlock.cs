using System.Numerics;
using NitroSharp.Saving;
using NitroSharp.Text;

namespace NitroSharp.Graphics
{
    internal sealed class TextBlock : RenderItem2D
    {
        private readonly string _markup;
        private readonly TextLayout _layout;
        private readonly DesignMarginU _margin;

        public TextBlock(
            in ResolvedEntityPath path,
            TextRenderContext ctx,
            int priority,
            string markup,
            DesignSizeU maxBounds,
            FontSettings fontSettings,
            in DesignMarginU margin)
            : base(path, priority)
        {
            _margin = margin;
            _markup = markup;
            _layout = CreateLayout(ctx, markup, maxBounds, fontSettings);
        }

        public TextBlock(in ResolvedEntityPath path, in TextBlockSaveData saveData, GameLoadingContext loadCtx)
            : base(path, saveData.Common)
        {
            _margin = saveData.Margin;
            _markup = saveData.Markup;
            _layout = CreateLayout(
                loadCtx.Rendering.Text,
                _markup,
                saveData.LayoutBounds,
                loadCtx.Process.FontSettings
            );
        }

        public override EntityKind Kind => EntityKind.TextBlock;

        private static TextLayout CreateLayout(
            TextRenderContext ctx,
            string markup,
            DesignSizeU maxBounds,
            FontSettings fontSettings)
        {
            TextSegment segment = Dialogue.ParseTextSegment(markup, fontSettings);
            var layout = new TextLayout(
                ctx.GlyphRasterizer,
                segment.TextRuns.AsSpan(),
                maxWidth: maxBounds.TypedWidth,
                maxHeight: maxBounds.TypedHeight,
                fixedLineHeight: null
            );
            ctx.RequestGlyphs(layout);
            return layout;
        }

        public override DesignSize GetUnconstrainedBounds(RenderContext ctx)
        {
            DesignRect bb = _layout.BoundingBox.Convert(ctx.DeviceToWorldScale);
            var size = new DesignSize(
                _margin.Left + bb.Right + _margin.Right,
                _margin.Top + bb.Bottom + _margin.Bottom
            );
            return size.Constrain(_layout.GetMaxDesignBounds(ctx).ToSizeF());
        }

        protected override void Update(GameContext ctx)
        {
            ctx.RenderContext.Text.RequestGlyphs(_layout);
        }

        protected override void LayoutPass(RenderContext ctx)
        {
            //Transform.Position = new Vector3(Transform.Position.X * 1.5f, Transform.Position.Y * 1.5f, Transform.Position.Z);
            base.LayoutPass(ctx);
        }

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            PhysicalRect rect = DeviceBoundingRect;

            PhysicalMarginU deviceMargin = _margin.Convert(ctx.WorldToDeviceScale);
            var offset = new PhysicalPoint(deviceMargin.Left, deviceMargin.Top);
            ctx.Text.Render(ctx, drawBatch, _layout, WorldMatrix, offset, rect, Color.A);

            return;

            ctx.MainBatch.PushQuad(
                QuadGeometry.Create(
                    BoundingRect.Size,
                    Matrix4x4.CreateTranslation(new Vector3(BoundingRect.Position, 0)),
                    Vector2.Zero,
                    Vector2.One,
                    new Vector4(0, 0.8f, 0.0f, 0.3f)
                ).Item1,
                ctx.WhiteTexture,
                ctx.WhiteTexture,
                default,
                BlendMode,
                ctx.GetSampler(FilterMode, DeviceBoundingRect.Size)
            );
        }

        public new TextBlockSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            Margin = _margin,
            Markup = _markup,
            LayoutBounds = _layout.GetMaxDesignBounds(ctx.RenderContext)
        };
    }

    [Persistable]
    internal readonly partial struct TextBlockSaveData : IEntitySaveData
    {
        public RenderItemSaveData Common { get; init; }
        public DesignMarginU Margin { get; init; }
        public string Markup { get; init; }
        public DesignSizeU LayoutBounds { get; init; }

        public EntitySaveData CommonEntityData => Common.EntityData;
    }
}
