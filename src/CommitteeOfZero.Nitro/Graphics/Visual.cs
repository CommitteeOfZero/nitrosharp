using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Graphics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public abstract class Visual : VisualBase
    {
        protected Visual()
        {
        }

        protected Visual(RgbaValueF color, float opacity, int priority)
        {
            Color = color;
            Opacity = opacity;
            Priority = priority;
        }

        protected Visual(RgbaValueF color, float opacity)
            : this(color, opacity, 0)
        {
        }

        protected Visual(RgbaValueF color)
            : this(color, 1.0f, 0)
        {
        }

       
        public int Priority { get; set; }

        public abstract void Render(ICanvas canvas);
        public virtual void Free(ICanvas canvas)
        {
        }
    }
}
