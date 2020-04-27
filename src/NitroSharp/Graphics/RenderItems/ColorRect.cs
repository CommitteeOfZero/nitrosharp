using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class ColorRect : RenderItem2D, TransitionSource
    {
        private readonly SizeF _size;

        public ColorRect(in ResolvedEntityPath path, int priority, SizeF size, in RgbaFloat color)
            : base(in path, priority)
        {
            _size = size;
            Color = color;
        }

        protected override SizeF GetUnconstrainedBounds(RenderContext ctx) => _size;

        public override void Render(RenderContext ctx)
        {
            ctx.PushQuad(
                ctx.DrawCommands,
                Quad,
                ctx.WhiteTexture,
                GetAlphaMask(ctx),
                BlendMode,
                FilterMode
            );
        }
    }
}
