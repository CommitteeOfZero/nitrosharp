using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class TextRect : RenderItem2D
    {
        public TextRect(
            in ResolvedEntityPath path,
            TextRenderContext ctx,
            int priority,
            TextLayout layout)
            : base(path, priority)
        {
            Layout = layout;
            ctx.RequestGlyphs(layout);
        }

        public TextLayout Layout { get; }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            RectangleF bb = Layout.BoundingBox;
            return new Size((uint)bb.Right, (uint)bb.Bottom);
        }

        public override void LayoutPass(World world, RenderContext ctx)
        {
            base.LayoutPass(world, ctx);
            ctx.Text.RequestGlyphs(Layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            ctx.Text.Render(ctx, drawBatch, Layout, WorldMatrix);
            RectangleF bb = Layout.BoundingBox;
            //drawBatch.PushQuad(
            //    QuadGeometry.Create(
            //        new SizeF(bb.Width, bb.Height),
            //        WorldMatrix * Matrix4x4.CreateTranslation(bb.X, bb.Y, 0),
            //        Vector2.Zero,
            //        Vector2.One,
            //        new Vector4(0, 0.8f, 0.0f, 0.3f)
            //    ).Item1,
            //    ctx.WhiteTexture,
            //    ctx.WhiteTexture,
            //    BlendMode,
            //    FilterMode
            //);
        }
    }
}
