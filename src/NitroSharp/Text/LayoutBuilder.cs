using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;

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
            var size = textRun.FontSize ?? 26;
            font.SetSize(size);

            string text = textRun.Text;
            uint idxRunStart = _glyphs.Count;
            ref var pen = ref _penPosition;
            for (uint i = 0; i < text.Length; i++)
            {
                char c = text[(int)i];
                ref var glyphInfo = ref font.GetGlyphInfo(c);
                ref var glyph = ref _glyphs.Count > i ? ref _glyphs[idxRunStart + i] : ref _glyphs.Add();
                glyph.Char = c;
                glyph.Color = textRun.Color;
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
                    glyph.Position = pen;
                    pen += glyphInfo.Advance;
                }
                else if (!char.IsWhiteSpace(c))
                {
                    while (!LineBreakingRules.CanStartLine(_glyphs[i].Char)
                        || !LineBreakingRules.CanEndLine(_glyphs[i - 1].Char))
                    {
                        i--;
                    }

                    i--;
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
