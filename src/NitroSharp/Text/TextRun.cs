using System;
using System.Runtime.InteropServices;
using Veldrid;

#nullable enable

namespace NitroSharp.Text
{
    [Flags]
    internal enum TextRunFlags
    {
        None,
        RubyText,
        Outline
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct TextRun
    {
        public readonly ReadOnlyMemory<char> Text;
        public readonly ReadOnlyMemory<char> RubyText;
        public readonly FontFaceKey Font;
        public readonly PtFontSize FontSize;
        public readonly RgbaFloat Color;
        public readonly RgbaFloat OutlineColor;
        public readonly TextRunFlags Flags;

        public bool DrawOutline => (Flags & TextRunFlags.Outline) == TextRunFlags.Outline;
        public bool HasRubyText => (Flags & TextRunFlags.RubyText) == TextRunFlags.RubyText;

        public static TextRun Regular(
            ReadOnlyMemory<char> text,
            FontFaceKey font, PtFontSize ptFontSize,
            RgbaFloat color, RgbaFloat? outlineColor)
        {
            return new TextRun(
                font, ptFontSize,
                color, outlineColor,
                text, rubyText: default
            );
        }

        public static TextRun WithRubyText(
            ReadOnlyMemory<char> rubyBase, ReadOnlyMemory<char> rubyText,
            FontFaceKey font, PtFontSize ptFontSize,
            RgbaFloat color, RgbaFloat? outlineColor)
        {
            return new TextRun(
                font, ptFontSize,
                color, outlineColor,
                rubyBase, rubyText
            );
        }

        private TextRun(
            FontFaceKey font, PtFontSize fontSize,
            RgbaFloat color, RgbaFloat? outlineColor,
            ReadOnlyMemory<char> text, ReadOnlyMemory<char> rubyText)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            OutlineColor = outlineColor ?? default;
            Text = text;
            RubyText = rubyText;
            Flags = TextRunFlags.None;
            if (rubyText.Length > 0)
            {
                Flags |= TextRunFlags.RubyText;
            }
            if (outlineColor is {})
            {
                Flags |= TextRunFlags.Outline;
            }
        }
    }
}
