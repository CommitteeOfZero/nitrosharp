using MoeGame.Framework;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public abstract class Visual : Component
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public RgbaValueF Color { get; set; }
        public float Opacity { get; set; }
        public int Priority { get; set; }

        public Visual()
        {
            Opacity = 1.0f;
        }

        public abstract void Render(ICanvas canvas);
        public virtual void Free(ICanvas canvas)
        {
        }
    }
}
