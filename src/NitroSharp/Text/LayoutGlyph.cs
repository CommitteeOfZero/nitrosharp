using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Text
{
    internal struct LayoutGlyph
    {
        public char Char;
        public SizeF Size;
        public Vector2 Advance;
        public Vector2 Position;
        public RgbaFloat? Color;
        public FontStyle FontStyle;
        public int? FontSize;
    }
}
