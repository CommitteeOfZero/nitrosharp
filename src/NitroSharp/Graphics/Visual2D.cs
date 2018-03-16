using Veldrid;

namespace NitroSharp.Graphics
{
    internal abstract class Visual2D : Visual
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

        public override void Render(RenderContext renderContext) => Render(renderContext.Canvas);
        public abstract void Render(Canvas canvas);
    }
}
