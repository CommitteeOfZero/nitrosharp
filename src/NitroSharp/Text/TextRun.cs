using System;
using System.Runtime.InteropServices;
using Veldrid;

#nullable enable

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
        public readonly FontKey Font;
        public readonly PtFontSize FontSize;
        public readonly RgbaFloat Color;
        public readonly RgbaFloat OutlineColor;
        public readonly TextRunFlags Flags;

        public bool HasRubyText => (Flags & TextRunFlags.RubyText) == TextRunFlags.RubyText;

        public static TextRun Regular(
            ReadOnlyMemory<char> text,
            FontKey font, PtFontSize ptFontSize,
            RgbaFloat color, RgbaFloat outlineColor)
        {
            return new TextRun(
                font, ptFontSize,
                color, outlineColor,
                text, rubyText: default,
                TextRunFlags.None
            );
        }

        public static TextRun WithRubyText(
            ReadOnlyMemory<char> rubyBase, ReadOnlyMemory<char> rubyText,
            FontKey font, PtFontSize ptFontSize,
            RgbaFloat color, RgbaFloat outlineColor)
        {
            return new TextRun(
                font, ptFontSize,
                color, outlineColor,
                rubyBase, rubyText,
                TextRunFlags.RubyText
            );
        }

        private TextRun(
            FontKey font, PtFontSize fontSize,
            RgbaFloat color, RgbaFloat outlineColor,
            ReadOnlyMemory<char> text, ReadOnlyMemory<char> rubyText,
            TextRunFlags flags)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            OutlineColor = outlineColor;
            Text = text;
            RubyText = rubyText;
            Flags = flags;
        }
    }
}
