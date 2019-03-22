using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace NitroSharp.Text
{
    [Flags]
    internal enum TextRunFlags : byte
    {
        None,
        RubyText
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct TextRun
    {
        public readonly ReadOnlyMemory<char> Text;
        public readonly ReadOnlyMemory<char> RubyText;
        public readonly FontFace Font;
        public readonly int FontSize;
        public readonly RgbaFloat Color;
        public readonly TextRunFlags Flags;

        public bool HasRubyText => (Flags & TextRunFlags.RubyText) == TextRunFlags.RubyText;

        public static TextRun Regular(
            ReadOnlyMemory<char> text, FontFace font, int ptFontSize, RgbaFloat color)
        {
            return new TextRun(font, ptFontSize, color, text, default, TextRunFlags.None);
        }

        public static TextRun WithRubyText(
            FontFace font, int ptFontSize, RgbaFloat color,
            ReadOnlyMemory<char> rubyBase, ReadOnlyMemory<char> rubyText)
        {
            return new TextRun(font, ptFontSize, color, rubyBase, rubyText, TextRunFlags.RubyText);
        }

        private TextRun(
            FontFace font,
            int fontSize,
            RgbaFloat color,
            ReadOnlyMemory<char> text,
            ReadOnlyMemory<char> rubyText,
            TextRunFlags flags)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            Text = text;
            RubyText = rubyText;
            Flags = flags;
        }
    }
}
