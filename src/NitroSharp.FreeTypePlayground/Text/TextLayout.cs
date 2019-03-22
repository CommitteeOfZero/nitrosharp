using System;
using System.Diagnostics;
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
        private int _currentLineIdx;

        private uint _lastWordStartGlyph;
        private float _lastWordStartGlyphX;

        private int _currentTextRunIdx;
        private ArrayBuilder<TextRun> _rubyTextRunsOnLine;

        public TextLayout(TextRun[] textRuns, Size maxBounds)
        {
            _glyphs = new ArrayBuilder<PositionedGlyph>(initialCapacity: 32);
            _lines = new ArrayBuilder<Line>(initialCapacity: 4);
            _lines.Add(new Line());
            _currentLineIdx = 0;
            _maxBounds = maxBounds;

            _prevBaselineY = 0;
            _lastWordStartGlyph = 0;
            _lastWordStartGlyphX = 0;
            _textRuns = textRuns;
            _currentTextRunIdx = 0;
            _rubyTextRunsOnLine = new ArrayBuilder<TextRun>(initialCapacity: 4);
            for (int i = 0; i < textRuns.Length; i++)
            {
                _currentTextRunIdx = i;
                Append(textRuns[i], i);
            }
        }

        public ReadOnlySpan<TextRun> TextRuns => _textRuns;
        public ReadOnlySpan<PositionedGlyph> Glyphs => _glyphs.AsSpan();

        private ref Line CurrentLine => ref _lines[_currentLineIdx];

        private bool Append(in TextRun textRun, int runIndex)
        {
            FontFace face = textRun.Font;
            ReadOnlySpan<char> text = textRun.Text.Span;
            uint glyphPos = _glyphs.Count;
            uint nbNonWhitespaceOnLine = 0;
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char c = text[stringPos];
                bool isNewline = c == '\r' || c == '\n';
                bool isWhitespace = char.IsWhiteSpace(c);
                Glyph glyph = textRun.Font.GetGlyph(c, textRun.FontSize);
                if (CanFitGlyph(glyph.Size) || isNewline)
                {
                    Debug.Assert(_glyphs.Count >= glyphPos);
                    var position = new Vector2(
                        CurrentLine.PenX + glyph.BitmapLeft,
                        glyph.BitmapTop
                    );

                    _glyphs.Add() = new PositionedGlyph(c, position, (ushort)runIndex);
                    if (!isWhitespace)
                    {
                        nbNonWhitespaceOnLine++;
                    }
                    bool wordStart;
                    if (!textRun.HasRubyText)
                    {
                        wordStart = LineBreakingRules.CanStartLine(c);
                        if (glyphPos > 0)
                        {
                            wordStart &= LineBreakingRules.CanEndLine(_glyphs[glyphPos - 1].Character);
                        }
                    }
                    else
                    {
                        // If a TextRun has ruby text, the base is treated as a single word
                        // to disallow line breaking. So only the first character of the base
                        // can start a word.
                        wordStart = stringPos == 0;
                    }
                    
                    if (wordStart)
                    {
                        _lastWordStartGlyph = glyphPos;
                        _lastWordStartGlyphX = CurrentLine.PenX;
                    }
                    else if (CurrentLine.Length == 0 || LineBreakingRules.CanEndLine(c) && nbNonWhitespaceOnLine > 0)
                    {
                        EndWord();
                    }

                    glyphPos++;
                    CurrentLine.Length++;
                    if (!isNewline)
                    {
                        CurrentLine.PenX += glyph.Advance.X;
                    }
                    else if (c == '\n')
                    {
                        if (nbNonWhitespaceOnLine > 0)
                        {
                            EndWord();
                        }
                        StartNewLine(face, glyphPos);
                        nbNonWhitespaceOnLine = 0;
                    }
                }
                else
                {
                    uint lastWordLength = glyphPos - _lastWordStartGlyph;
                    float lastWordWidth = CurrentLine.PenX - _lastWordStartGlyphX;
                    CurrentLine.Length -= lastWordLength;
                    // Start a new line and move the last word to it
                    if (!StartNewLine(face, _lastWordStartGlyph))
                    {
                        return false;
                    }
                    nbNonWhitespaceOnLine = 0;
                    for (uint i = _lastWordStartGlyph; i < glyphPos; i++)
                    {
                        ref PositionedGlyph g = ref _glyphs[i];
                        g = new PositionedGlyph(
                            g.Character,
                            new Vector2(g.Position.X - _lastWordStartGlyphX, g.Position.Y),
                            g.TextRunIndex
                        );

                        CurrentLine.Length++;
                        nbNonWhitespaceOnLine++;
                    }

                    CurrentLine.PenX = lastWordWidth;
                    // The one character that couldn't fit on the previous line
                    // and thus triggered a line break still needs to be positioned.
                    stringPos--;
                }
            }

            EndWord();
            return true;
        }

        private void EndWord()
        {
            ref Line line = ref CurrentLine;
            ref readonly TextRun textRun = ref _textRuns[_currentTextRunIdx];
            line.LargestFontSize = Math.Max(
                line.LargestFontSize,
                textRun.FontSize
            );

        }

        public bool EndLine(FontFace font)
        {
            ref Line line = ref CurrentLine;
            Debug.Assert(line.LargestFontSize > 0);
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

        private bool CalculateBaselineY(out float baselineY)
        {
            ref VerticalMetrics largestMetrics = ref CurrentLine.LargestFontMetrics;
            if (_currentLineIdx > 0)
            {
                ref Line prevLine = ref _lines[_currentLineIdx - 1];
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

            return (baselineY + Math.Abs(largestMetrics.Descender)) <= _maxBounds.Height;
        }

        public bool StartNewLine(FontFace font, uint startGlyph, float baselineX = 0)
        {
            if (EndLine(font))
            {
                _lines.Add();
                _currentLineIdx++;
                CurrentLine.Start = startGlyph;
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
