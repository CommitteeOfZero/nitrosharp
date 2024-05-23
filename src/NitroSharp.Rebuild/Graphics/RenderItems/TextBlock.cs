using System.Numerics;
using NitroSharp.Text;

namespace NitroSharp.Graphics;

internal sealed class TextBlock : RenderItem
{
    private readonly string _markup;
    private readonly TextLayout _layout;
    private readonly Vector4 _margin;

    public TextBlock(
        EntityName name,
        Entity? parent,
        TextRenderContext ctx,
        int priority,
        string markup,
        DesignSizeU maxBounds,
        FontSettings fontSettings,
        in Vector4 margin)
        : base(name, parent, priority)
    {
        _margin = margin;
        _markup = markup;
        _layout = CreateLayout(ctx, markup, maxBounds, fontSettings);
    }

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
            maxBounds.Width,
            maxBounds.Height
        );
        ctx.RequestGlyphs(layout);
        return layout;
    }

    public override DesignSize GetSize(RenderContext ctx)
    {
        DesignRect bb = _layout.BoundingBox;
        var size = new DesignSize(
            _margin.X + bb.Right + _margin.Z,
            _margin.Y + bb.Bottom + _margin.W
        );
        return size.Constrain(_layout.MaxBounds);
    }

    public override void Update(GameContext ctx)
    {
        base.Update(ctx);
        ctx.RenderContext.Text.RequestGlyphs(_layout);
    }

    public override void Render(GameContext ctx)
    {
        RenderContext renderContext = ctx.RenderContext;

        DesignSize size = GetSize(renderContext);
        renderContext.Text.Render(
            ctx.RenderContext,
            ctx.RenderContext.MainBatch,
            _layout,
            ctx.RenderContext.GetTransformMatrix(Transform, size, useScaling: true, aligned: false),
            _margin.XY(),
            null,
            Color.A
        );
    }
}
