using System.Drawing;

namespace CommitteeOfZero.Nitro.Foundation.Graphics
{
    public abstract class VisualBase : Component
    {
        public RgbaValueF Color { get; set; }
        public float Opacity { get; set; }

        public virtual SizeF Measure()
        {
            return SizeF.Empty;
        }
    }
}
