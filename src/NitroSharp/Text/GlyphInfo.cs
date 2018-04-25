using System.Numerics;
using NitroSharp.Primitives;

namespace NitroSharp.Text
{
    internal struct GlyphInfo
    {
        public Vector2 Advance;
        public SizeF Size;
        public unsafe FreeTypeBindings.Glyph* FTGlyph;
    }
}
