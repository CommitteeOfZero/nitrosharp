using System.Numerics;
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
            //RectangleF bb = layout.BoundingBox;
            //LocalBounds = new SizeF(bb.Right, bb.Bottom);
        }

        public TextLayout Layout { get; }

        protected override SizeF GetUnconstrainedBounds(RenderContext ctx)
        {
            return Layout.BoundingBox.Size;
        }

        public override void LayoutPass(World world, RenderContext ctx)
        {
            base.LayoutPass(world, ctx);
            ctx.Text.RequestGlyphs(Layout);

            RectangleF bb = Layout.BoundingBox;
            //ctx.PushQuad(
            //    QuadGeometry.Create(
            //        new SizeF(bb.Width, bb.Height),
            //        Transform.Matrix * Matrix4x4.CreateTranslation(bb.X, bb.Y, 0),
            //        Vector2.Zero,
            //        Vector2.One,
            //        new Vector4(0, 0.8f, 0.0f, 0.3f),
            //        out _
            //    ),
            //    ctx.WhiteTexture,
            //    ctx.WhiteTexture,
            //    BlendMode,
            //    FilterMode
            //);
        }

        public override void Render(RenderContext ctx)
        {
            ctx.Text.Render(ctx, Layout, WorldMatrix);
        }
    }
}
