using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Text
{
    internal struct LayoutGlyph
    {
        public char Char;
        public Vector2 Position;
        public RgbaFloat? Color;
        public FontStyle FontStyle;
    }
}
