using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp.Text
{
    internal readonly struct PositionedGlyph
    {
        public readonly Vector2 Position;
        public readonly char Character;
        public readonly ushort TextRunIndex;

        public PositionedGlyph(char character, Vector2 position, ushort textRunIndex)
        {
            Character = character;
            Position = position;
            TextRunIndex = textRunIndex;
        }

        public override string ToString() => $"{{'{Character}', {Position}}}";
    }

    internal struct TextLayout
    {
        [StructLayout(LayoutKind.Auto)]
        private struct Line
        {
            public uint Start;
            public uint Length;
            public float PenX;
            public int LargestFontSize;
            public VerticalMetrics LargestFontMetrics;
        }

        private readonly Size _maxBounds;
        private readonly TextRun[] _textRuns;
        private ArrayBuilder<Line> _lines;

        private ArrayBuilder<PositionedGlyph> _glyphs;
        private float _prevBaselineY;
        private int _currentLine;

        public TextLayout(TextRun[] textRuns, Size maxBounds)
        {
            _glyphs = new ArrayBuilder<PositionedGlyph>(initialCapacity: 32);
            _lines = new ArrayBuilder<Line>(initialCapacity: 4);
            _lines.Add(new Line());
            _currentLine = 0;
            _maxBounds = maxBounds;

            _prevBaselineY = 0;
            _textRuns = textRuns;
            for (int i = 0; i < textRuns.Length; i++)
            {
                Append(textRuns[i].Regular, i);
            }
        }

        public ReadOnlySpan<TextRun> TextRuns => _textRuns;
        public ReadOnlySpan<PositionedGlyph> Glyphs => _glyphs.AsSpan();

        private ref Line CurrentLine => ref _lines[_currentLine];

        private bool Append(in RegularTextRun textRun, int runIndex)
        {
            FontFace face = textRun.Font;
            ReadOnlySpan<char> text = textRun.Text.Span;

            // There is no one-to-one mapping between characters and glyps.
            // For instance, whitespace characters do not have any corresponding glyphs.
            // So we need 2 variables, charPos and glyphPos, to track the current position in the text.
            uint glyphPos = _glyphs.Count;
            for (int charPos = 0; charPos < text.Length; charPos++)
            {
                char c = text[charPos];
                bool isWhitespace = char.IsWhiteSpace(c);
                Glyph glyph = textRun.Font.GetGlyph(c, textRun.FontSize);

                if ((charPos > 0 && LineBreakingRules.CanEndLine(c)) || charPos == text.Length - 1)
                {
                    CurrentLine.LargestFontSize = Math.Max(
                       CurrentLine.LargestFontSize,
                       textRun.FontSize
                   );
                }

                if (c == '\n')
                {
                    if (!StartNewLine(face, start: glyphPos))
                    {
                        return false;
                    }

                    continue;
                }

                if (isWhitespace)
                {
                    CurrentLine.PenX += glyph.Advance.X;
                    continue;
                }

                if (CanFitGlyph(glyph.Size))
                {
                    ref PositionedGlyph positionedGlyph = ref _glyphs.Count > glyphPos
                        ? ref _glyphs[glyphPos]
                        : ref _glyphs.Add();

                    var position = new Vector2(
                        CurrentLine.PenX + glyph.BitmapLeft,
                        glyph.BitmapTop
                    );

                    positionedGlyph = new PositionedGlyph(c, position, (ushort)runIndex);
                    glyphPos++;
                    CurrentLine.Length++;
                    CurrentLine.PenX += glyph.Advance.X;
                }
                else
                {
                    if (!CanFitCurrentLine(face))
                    {
                        return false;
                    }

                    while (charPos >= 1 &&
                        (!LineBreakingRules.CanStartLine(text[charPos])
                         || !LineBreakingRules.CanEndLine(text[charPos - 1])))
                    {
                        charPos--;
                        if (!char.IsWhiteSpace(text[charPos]))
                        {
                            glyphPos--;
                            CurrentLine.Length--;
                        }
                    }
                    charPos--;
                    if (!StartNewLine(face, glyphPos))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool EndLine(FontFace font)
        {
            ref Line line = ref CurrentLine;
            line.LargestFontMetrics = font.GetVerticalMetrics(line.LargestFontSize);
            if (!CalculateBaselineY(out float baselineY))
            {
                _glyphs.Count -= line.Length;
                line.Length = 0;
                return false;
            }

            var span = _glyphs.AsSpan((int)line.Start, (int)CurrentLine.Length);
            foreach (ref PositionedGlyph glyph in span)
            {
                glyph = new PositionedGlyph(
                    glyph.Character,
                    new Vector2(glyph.Position.X, baselineY - glyph.Position.Y),
                    glyph.TextRunIndex
                );
            }

            _prevBaselineY = baselineY;
            return true;
        }

        private bool CanFitCurrentLine(FontFace font)
        {
            ref Line line = ref CurrentLine;
            line.LargestFontMetrics = font.GetVerticalMetrics(line.LargestFontSize);
            return CalculateBaselineY(out _);
        }

        private bool CalculateBaselineY(out float baselineY)
        {
            ref VerticalMetrics largestMetrics = ref CurrentLine.LargestFontMetrics;
            if (_currentLine > 0)
            {
                ref Line prevLine = ref _lines[_currentLine - 1];
                ref VerticalMetrics prevLargestMetrics = ref prevLine.LargestFontMetrics;
                baselineY = _prevBaselineY
                    + largestMetrics.Ascender
                    - prevLargestMetrics.Descender
                    + prevLargestMetrics.LineGap;
            }
            else
            {
                baselineY = _prevBaselineY + largestMetrics.Ascender;
            }

            return baselineY + Math.Abs(largestMetrics.Descender) <= _maxBounds.Height;
        }

        public bool StartNewLine(FontFace font, uint start, float baselineX = 0)
        {
            if (EndLine(font))
            {
                _lines.Add();
                _currentLine++;
                CurrentLine.Start = start;
                CurrentLine.PenX = baselineX;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanFitGlyph(in SizeF size)
            => CurrentLine.PenX + size.Width <= _maxBounds.Width;
    }
}
