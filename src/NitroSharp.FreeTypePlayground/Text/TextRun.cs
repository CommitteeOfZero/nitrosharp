using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace NitroSharp.Text
{
    internal enum TextRunKind
    {
        Regular,
        RubyText
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct TextRun
    {
        [FieldOffset(0)]
        public readonly TextRunKind Kind;

        [FieldOffset(8)]
        public readonly RegularTextRun Regular;

        [FieldOffset(8)]
        public readonly RubyTextRun RubyText;

        public static TextRun MakeRegular(
            ReadOnlyMemory<char> text, FontFace font, int ptFontSize, RgbaFloat color)
        {
            return new TextRun(new RegularTextRun(text, font, ptFontSize, color));
        }

        public static TextRun MakeRuby(
            FontFace font, int ptFontSize, RgbaFloat color,
            ReadOnlyMemory<char> rubyBase, ReadOnlyMemory<char> rubyText)
        {
            return new TextRun(
                new RubyTextRun(font, ptFontSize, color, rubyBase, rubyText));
        }

        private TextRun(RegularTextRun regularTextRun) : this()
            => (Kind, Regular) = (TextRunKind.Regular, regularTextRun);

        private TextRun(RubyTextRun rubyTextRun) : this()
            => (Kind, RubyText) = (TextRunKind.RubyText, rubyTextRun);
    }

    internal readonly struct RegularTextRun
    {
        public readonly FontFace Font;
        public readonly RgbaFloat Color;
        public readonly int FontSize;
        public readonly ReadOnlyMemory<char> Text;

        public RegularTextRun(ReadOnlyMemory<char> text, FontFace font, int fontSize, RgbaFloat color)
            => (Text, Font, FontSize, Color) = (text, font, fontSize, color);
    }

    internal readonly struct RubyTextRun
    {
        public readonly FontFace Font;
        public readonly int FontSize;
        public readonly RgbaFloat Color;
        public readonly ReadOnlyMemory<char> RubyBase;
        public readonly ReadOnlyMemory<char> RubyText;

        public RubyTextRun(
            FontFace font, int fontSize, RgbaFloat color,
            ReadOnlyMemory<char> rubyBase, ReadOnlyMemory<char> rubyText)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            RubyBase = rubyBase;
            RubyText = rubyText;
        }
    }
}
