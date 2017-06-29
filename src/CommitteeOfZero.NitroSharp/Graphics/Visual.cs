using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Graphics;
using System;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public abstract class Visual : VisualBase
    {
        protected Visual()
        {
            Opacity = 1.0f;
        }

        protected Visual(RgbaValueF color, float opacity, int priority)
        {
            Color = color;
            Opacity = opacity;
            Priority = priority;
        }

        protected Visual(RgbaValueF color, int priority)
            : this(color, 1.0f, priority)
        {

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

        public abstract void Render(INitroRenderer nitroRenderer);
        public virtual void Free(INitroRenderer nitroRenderer)
        {
        }
    }
}
