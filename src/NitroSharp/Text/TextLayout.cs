using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Text
{
    internal sealed class TextLayout
    {
        public ArrayBuilder<LayoutGlyph> Glyphs;
        // TODO: implement GPU caching instead of using dirty flags to reduce CPU work.
        public HashSet<ushort> DirtyGlyphs;
        public readonly FontFamily FontFamily;
        public readonly Size MaxBounds;
        public Vector2 PenPosition;
        private FontFace _lastFont;

        public TextLayout(FontFamily fontFamily, Size maxBounds, uint initialCapacity)
        {
            Glyphs = new ArrayBuilder<LayoutGlyph>(initialCapacity);
            DirtyGlyphs = new HashSet<ushort>();
            FontFamily = fontFamily;
            MaxBounds = maxBounds;
            PenPosition = Vector2.Zero;
            _lastFont = null;
        }

        public void Append(Span<TextRun> textRuns)
        {
            for (int i = 0; i < textRuns.Length; i++)
            {
                if (!Append(textRuns[i]))
                {
                    return;
                }
            }
        }

        public bool Append(TextRun textRun, bool display = true)
        {
            FontFace font = _lastFont = FontFamily.GetFace(textRun.FontStyle);
            int size = textRun.FontSize ?? 28;
            font.SetSize(size);

            string text = textRun.Text;
            ref Vector2 pen = ref PenPosition;

            // There is no one-to-one mapping between characters in the original string and LayoutGlyphs
            // in the output glyph array, as whitespace characters do not have any corresponding glyphs.
            // So we need 2 variables, stringPos and glyphPos, to track the current position in the text.
            uint glyphPos = Glyphs.Count;
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char c = text[stringPos];
                bool isWhitespace = char.IsWhiteSpace(c);

                ref GlyphInfo glyphInfo = ref font.GetGlyphInfo(c);
                ref LayoutGlyph glyph = ref Glyphs.Count > glyphPos ? ref Glyphs[glyphPos] : ref Glyphs.Add();
                glyph.Char = c;
                glyph.Color = textRun.Color ?? RgbaFloat.White;
                glyph.FontStyle = textRun.FontStyle;

                if (c == '\n')
                {
                    if (!StartNewLine())
                    {
                        Glyphs.RemoveLast();
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
                    while (stringPos >= 1 &&
                        (!LineBreakingRules.CanStartLine(text[stringPos])
                        || !LineBreakingRules.CanEndLine(text[stringPos - 1])))
                    {
                        stringPos--;
                        if (!char.IsWhiteSpace(text[stringPos]))
                        {
                            glyphPos--;
                        }
                    }

                    stringPos--;
                    if (!StartNewLine())
                    {
                        Glyphs.RemoveLast();
                        return false;
                    }
                }
            }

            if (display)
            {
                for (ushort i = 0; i < Glyphs.Count; i++)
                {
                    DirtyGlyphs.Add(i);
                }
            }

            return true;
        }

        public bool StartNewLine()
        {
            ref Vector2 pen = ref PenPosition;
            FontMetrics metrics = _lastFont.ScaledMetrics;
            if ((pen.Y + metrics.LineHeight * 2) <= MaxBounds.Height)
            {
                pen.X = 0;
                pen.Y += _lastFont.ScaledMetrics.LineHeight;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            Glyphs.Reset();
            PenPosition = default;
        }

        public ref LayoutGlyph MutateGlyph(ushort index)
        {
            if (index >= Glyphs.Count)
            {
                ThrowOutOfRange();
            }

            DirtyGlyphs.Add(index);
            return ref Glyphs[index];
        }

        public Span<LayoutGlyph> MutateSpan(ushort start, ushort length)
        {
            if ((start + length) > Glyphs.Count)
            {
                ThrowOutOfRange();
            }

            Span<LayoutGlyph> span = Glyphs.AsSpan().Slice(start, length);
            for (ushort i = start; i < start + length; i++)
            {
                DirtyGlyphs.Add(i);
            }

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanFitGlyph(in SizeF size)
        {
            return PenPosition.X + size.Width <= MaxBounds.Width;
        }

        private void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
    }
}
