using MoeGame.Framework;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TextDrawingContext : ComObject
    {
        public float OpacityOverride { get; set; }
        public RgbaValueF ColorOverride { get; set; }
    }
}
