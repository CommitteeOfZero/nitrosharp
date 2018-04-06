using System;
using Veldrid;

namespace NitroSharp.Text
{
    internal struct TextRun
    {
        public string Text;
        public int? FontSize;
        public RgbaFloat? Color;
        public RgbaFloat? ShadowColor;
        public FontStyle FontStyle;
    }
}
