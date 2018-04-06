using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal abstract class Visual : Component
    {
        protected RgbaFloat _color = RgbaFloat.White;

        protected Visual()
        {
        }

        protected Visual(RgbaFloat color, float opacity, int priority)
        {
            Color = color;
            Opacity = opacity;
            Priority = priority;
        }

        protected Visual(RgbaFloat color, int priority)
            : this(color, 1.0f, priority)
        {

        }

        protected Visual(RgbaFloat color, float opacity)
            : this(color, opacity, 0)
        {
        }

        protected Visual(RgbaFloat color)
            : this(color, 1.0f, 0)
        {
        }

        public virtual float Opacity
        {
            get => Color.A;
            set => Color = new RgbaFloat(Color.R, Color.G, Color.B, value);
        }

        public virtual RgbaFloat Color
        {
            get => _color;
            protected set => _color = value;
        }

        public int Priority { get; protected set; }
        public virtual SizeF Bounds => SizeF.Zero;

        public virtual void CreateDeviceObjects(RenderContext renderContext)
        {
        }

        public virtual void Destroy(RenderContext renderContext)
        {
        }

        public abstract void Render(RenderContext renderContext);
    }
}
