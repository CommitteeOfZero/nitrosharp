using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class TextRect : RenderItem2D
    {
        public TextRect(
            in ResolvedEntityPath path,
            int priority,
            TextRenderContext ctx,
            TextLayout layout)
            : base(in path, priority)
        {
            Layout = layout;
            ctx.RequestGlyphs(layout);
            Bounds = new SizeF(200, 200);
        }

        public TextLayout Layout { get; }

        public override void Update(World world, RenderContext ctx)
        {
            base.Update(world, ctx);
            ctx.Text.RequestGlyphs(Layout);
        }

        public override void Render(RenderContext ctx)
        {
            ctx.Text.Render(ctx, Layout, Transform.Matrix);
        }
    }
}
