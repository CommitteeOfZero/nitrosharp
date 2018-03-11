using NitroSharp.Graphics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal abstract class Visual2D : Component, Renderable
    {
        protected Visual2D()
        {
            Opacity = 1.0f;
        }

        protected Visual2D(RgbaFloat color, float opacity, int priority)
        {
            Color = color;
            Opacity = opacity;
            Priority = priority;
        }

        protected Visual2D(RgbaFloat color, int priority)
            : this(color, 1.0f, priority)
        {

        }

        protected Visual2D(RgbaFloat color, float opacity)
            : this(color, opacity, 0)
        {
        }

        protected Visual2D(RgbaFloat color)
            : this(color, 1.0f, 0)
        {
        }

        public float Opacity { get; set; }
        public RgbaFloat Color { get; }
        public int Priority { get; }

        public abstract SizeF Bounds { get; }

        public abstract void Render(Canvas canvas);
    }
}
