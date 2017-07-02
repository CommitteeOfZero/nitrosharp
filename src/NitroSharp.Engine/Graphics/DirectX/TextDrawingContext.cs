using NitroSharp.Foundation;
using SharpDX;

namespace NitroSharp.Graphics
{
    public class TextDrawingContext : ComObject
    {
        public float OpacityOverride { get; set; }
        public RgbaValueF ColorOverride { get; set; }
    }
}
