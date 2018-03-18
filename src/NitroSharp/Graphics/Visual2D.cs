using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal abstract class Visual2D : Visual
    {
        protected Visual2D()
        {
        }

        protected Visual2D(RgbaFloat color) : base(color)
        {
        }

        protected Visual2D(RgbaFloat color, int priority) : base(color, priority)
        {
        }

        protected Visual2D(RgbaFloat color, float opacity) : base(color, opacity)
        {
        }

        protected Visual2D(RgbaFloat color, float opacity, int priority) : base(color, opacity, priority)
        {
        }

        public override abstract SizeF Bounds { get; }

        public override void Render(RenderContext renderContext) => Render(renderContext.Canvas);
        public abstract void Render(Canvas canvas);
    }
}
