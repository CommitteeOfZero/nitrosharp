using System;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal abstract class Visual : Component
    {
        protected Visual()
        {
            Opacity = 1.0f;
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

        public float Opacity { get; set; }
        public RgbaFloat Color { get; }
        public int Priority { get; protected set; }
        public virtual SizeF Bounds => throw new InvalidOperationException();

        public virtual void CreateDeviceObjects(RenderContext renderContext)
        {
        }

        public virtual void DestroyDeviceResources(RenderContext renderContext)
        {
        }

        public abstract void Render(RenderContext renderContext);
    }
}
