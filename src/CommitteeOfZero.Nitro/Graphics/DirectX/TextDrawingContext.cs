using MoeGame.Framework;
using SharpDX;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TextDrawingContext : ComObject
    {
        public float OpacityOverride { get; set; }
        public RgbaValueF ColorOverride { get; set; }
    }
}
