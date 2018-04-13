using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Text
{
    internal sealed class LayoutBuilder
    {
        private ValueList<LayoutGlyph> _glyphs;
        private readonly FontFamily _fontFamily;
        private Vector2 _penPosition;
        private Size _maxLayoutBounds;

        public LayoutBuilder(FontFamily fontFamily, uint initialCapacity, in Size maxBounds)
        {
            _fontFamily = fontFamily;
            _penPosition = Vector2.Zero;
            _maxLayoutBounds = maxBounds;
            _glyphs = new ValueList<LayoutGlyph>(initialCapacity);
        }

        public ref ValueList<LayoutGlyph> Glyphs => ref _glyphs;

        public void Append(TextRun[] textRuns)
        {
            for (int i = 0; i < textRuns.Length; i++)
            {
                if (!AppendTextRun(ref textRuns[i]))
                {
                    return;
                }
            }
        }

        private bool AppendTextRun(ref TextRun textRun)
        {
            var font = _fontFamily.GetFace(textRun.FontStyle);
            var size = textRun.FontSize ?? 28;
            font.SetSize(size);

            string text = textRun.Text;
            ref var pen = ref _penPosition;

            // There is no one-to-one mapping between characters in the original string and LayoutGlyphs
            // in the output glyph array, as whitespace characters do not have any corresponding glyphs.
            // So we need 2 variables, stringPos and glyphPos, to track the current position in the text.
            uint glyphPos = _glyphs.Count;
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char c = text[stringPos];
                bool isWhitespace = char.IsWhiteSpace(c);

                ref var glyphInfo = ref font.GetGlyphInfo(c);
                ref var glyph = ref _glyphs.Count > glyphPos ? ref _glyphs[glyphPos] : ref _glyphs.Add();
                glyph.Char = c;
                glyph.Color = textRun.Color ?? RgbaFloat.White;
                glyph.FontStyle = textRun.FontStyle;

                if (c == '\n')
                {
                    if (!StartNewLine(font))
                    {
                        _glyphs.RemoveLast();
                        return false;
                    }

                    continue;
                }

                if (CanFitGlyph(glyphInfo.Size))
                {
                    if (!isWhitespace)
                    {
                        glyph.Position = pen;
                        glyphPos++;
                    }

                    pen += glyphInfo.Advance;
                }
                else if (!isWhitespace)
                {
                    while (!LineBreakingRules.CanStartLine(_glyphs[glyphPos].Char)
                        || !LineBreakingRules.CanEndLine(_glyphs[glyphPos - 1].Char))
                    {
                        glyphPos--;
                        while (char.IsWhiteSpace(text[stringPos]))
                        {
                            stringPos--;
                        }
                    }

                    stringPos--;
                    glyphPos--;
                    if (!StartNewLine(font))
                    {
                        _glyphs.RemoveLast();
                        return false;
                    }
                }
            }

            return true;
        }

        private bool StartNewLine(FontFace fontFace)
        {
            ref var pen = ref _penPosition;
            var metrics = fontFace.ScaledMetrics;
            if ((pen.Y + metrics.LineHeight * 2) <= _maxLayoutBounds.Height)
            {
                pen.X = 0;
                pen.Y += fontFace.ScaledMetrics.LineHeight;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanFitGlyph(in SizeF size)
        {
            return _penPosition.X + size.Width <= _maxLayoutBounds.Width;
        }
    }
}
