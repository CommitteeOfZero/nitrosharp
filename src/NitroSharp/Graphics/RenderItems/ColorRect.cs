using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ColorRect : RenderItem2D
    {
        private readonly Size _size;

        public ColorRect(
            in ResolvedEntityPath path,
            int priority,
            Size size,
            in RgbaFloat color)
            : base(path, priority)
        {
            _size = size;
            Color = color;
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx) => _size;

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            drawBatch.PushQuad(
                Quad,
                ctx.WhiteTexture,
                GetAlphaMask(ctx),
                BlendMode,
                FilterMode
            );
        }
    }
}
