using System.Numerics;
using NitroSharp.Primitives;

namespace NitroSharp.Text
{
    internal readonly struct GlyphBitmapInfo
    {
        public GlyphBitmapInfo(in Size dimensions, in Vector2 margin)
        {
            Dimensions = dimensions;
            Margin = margin;
        }

        public Size Dimensions { get; }
        public Vector2 Margin { get; }
    }
}
