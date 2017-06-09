using System.Drawing;
using CommitteeOfZero.Nitro.Foundation;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class RectangleVisual : Visual
    {
        public RectangleVisual(SizeF size, RgbaValueF color, float opacity, int priority)
            : base(color, opacity, priority)
        {
            Size = size;
        }

        public RectangleVisual(float width, float height, RgbaValueF color, float opacity, int priority)
            : base(color, opacity, priority)
        {
            Size = new SizeF(width, height);
        }

        public SizeF Size { get; set; }
        public float Width => Size.Width;
        public float Height => Size.Height;

        public override SizeF Measure()
        {
            return Size;
        }

        public override void Render(INitroRenderer renderer)
        {
            renderer.DrawRectangle(this);
        }
    }
}
