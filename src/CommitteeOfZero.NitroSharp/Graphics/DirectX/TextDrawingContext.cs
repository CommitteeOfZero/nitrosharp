using CommitteeOfZero.NitroSharp.Foundation;
using SharpDX;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public class TextDrawingContext : ComObject
    {
        public float OpacityOverride { get; set; }
        public RgbaValueF ColorOverride { get; set; }
    }
}
