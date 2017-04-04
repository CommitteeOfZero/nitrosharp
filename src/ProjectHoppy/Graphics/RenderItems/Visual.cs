using HoppyFramework;
using HoppyFramework.Graphics;

namespace ProjectHoppy.Graphics.RenderItems
{
    public abstract class Visual : RenderItem
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public RgbaValueF Color { get; set; }
        public float Opacity { get; set; } = 1.0f;
        public int Priority { get; set; }
    }
}
