using System.Numerics;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class TextBlock : RenderItem2D
    {
        private readonly TextLayout _layout;

        public TextBlock(
            in ResolvedEntityPath path,
            TextRenderContext ctx,
            int priority,
            TextLayout layout,
            in Vector4 margin)
            : base(path, priority)
        {
            _layout = layout;
            Margin = margin;
            ctx.RequestGlyphs(layout);
        }

        public Vector4 Margin { get; }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            RectangleF bb = _layout.BoundingBox;
            var size = new Size(
                (uint)(Margin.X + bb.Right + Margin.Z),
                (uint)(Margin.Y + bb.Bottom + Margin.W)
            );
            return size.Constrain(_layout.MaxBounds);
        }

        protected override void Update(GameContext ctx)
        {
            ctx.RenderContext.Text.RequestGlyphs(_layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            RectangleF br = BoundingRect;
            var rect = new RectangleU((uint)br.X, (uint)br.Y, (uint)br.Width, (uint)br.Height);
            ctx.Text.Render(ctx, drawBatch, _layout, WorldMatrix, Margin.XY(), rect);
            RectangleF bb = _layout.BoundingBox;
            //ctx.MainBatch.PushQuad(
            //    QuadGeometry.Create(
            //        new SizeF(bb.Size.Width, bb.Size.Height),
            //        WorldMatrix * Matrix4x4.CreateTranslation(new Vector3(Margin.XY() + bb.Position, 0)),
            //        Vector2.Zero,
            //        Vector2.One,
            //        new Vector4(0, 0.8f, 0.0f, 0.3f)
            //    ).Item1,
            //    ctx.WhiteTexture,
            //    ctx.WhiteTexture,
            //    default,
            //    BlendMode,
            //    FilterMode
            //);

            var rasterizer = ctx.Text.GlyphRasterizer;
            foreach (var glyphRun in _layout.GlyphRuns)
            {
                var glyphs = _layout.GetGlyphs(glyphRun.GlyphSpan);
                var font = rasterizer.GetFontData(glyphRun.Font);
                foreach (PositionedGlyph g in glyphs)
                {
                    var dims = font.GetGlyphDimensions(g.Index, glyphRun.FontSize);

                    //ctx.MainBatch.PushQuad(
                    //    QuadGeometry.Create(
                    //        new SizeF(dims.Width, dims.Height),
                    //        WorldMatrix * Matrix4x4.CreateTranslation(new Vector3(Margin.XY() + g.Position + new Vector2(0, 0), 0)),
                    //        Vector2.Zero,
                    //        Vector2.One,
                    //        new Vector4(0.8f, 0.0f, 0.0f, 0.3f)
                    //    ).Item1,
                    //    ctx.WhiteTexture,
                    //    ctx.WhiteTexture,
                    //    default,
                    //    BlendMode,
                    //    FilterMode
                    //);
                }
            }
        }
    }
}
