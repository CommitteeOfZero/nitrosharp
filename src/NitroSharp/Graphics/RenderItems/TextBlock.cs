using System.Numerics;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class TextBlock : RenderItem2D
    {
        public TextBlock(
            in ResolvedEntityPath path,
            TextRenderContext ctx,
            int priority,
            TextLayout layout,
            in Vector4 padding)
            : base(path, priority)
        {
            Layout = layout;
            Padding = padding;
            ctx.RequestGlyphs(layout);
        }

        public TextLayout Layout { get; }
        public Vector4 Padding { get; }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            RectangleF bb = Layout.BoundingBox;
            return new Size(
                (uint)(bb.Right + Padding.X + Padding.Z),
                (uint)(bb.Bottom + Padding.Y + Padding.W)
            );
        }

        protected override void LayoutPass(RenderContext ctx)
        {
            base.LayoutPass(ctx);
            ctx.Text.RequestGlyphs(Layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            ctx.Text.Render(ctx, drawBatch, Layout, WorldMatrix, new Vector2(Padding.X, Padding.Y));
            //drawBatch.PushQuad(
            //    QuadGeometry.Create(
            //        GetUnconstrainedBounds(ctx).ToSizeF(),
            //        WorldMatrix,
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
