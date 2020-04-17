using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Text
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct GlyphRun
    {
        public readonly FontKey Font;
        public readonly PtFontSize FontSize;
        public readonly RgbaFloat Color;
        public readonly RgbaFloat OutlineColor;
        public readonly GlyphSpan GlyphSpan;
        public readonly bool IsRuby;

        public GlyphRun(
            FontKey font,
            PtFontSize fontSize,
            RgbaFloat color,
            RgbaFloat outlineColor,
            GlyphSpan glyphSpan,
            bool isRuby)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            OutlineColor = outlineColor;
            GlyphSpan = glyphSpan;
            IsRuby = isRuby;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct PositionedGlyph
    {
        public readonly uint Index;
        public readonly Vector2 Position;

        public PositionedGlyph(uint index, Vector2 position)
        {
            Index = index;
            Position = position;
        }

        public override int GetHashCode() => HashCode.Combine(Index, Position);
        public override string ToString() => $"{{Glyph #{Index}, {Position}}}";
    }

    internal sealed class TextLayout
    {
        [StructLayout(LayoutKind.Auto)]
        private struct Line
        {
            public uint Start;
            public uint Length;
            public uint GlyphsPositioned;
            public float PenX;
            public float? BaselineY;
            public PtFontSize LargestFontSize;
            public VerticalMetrics LargestFontMetrics;
        }

        [StructLayout(LayoutKind.Auto)]
        private struct RubyTextChunk
        {
            public uint TextRun;
            public GlyphSpan RubyBaseSpan;
            public GlyphSpan RubyTextSpan;
            public bool BeenProcessed;
        }

        private readonly Size _maxBounds;
        private readonly float _rubyFontSizeMultiplier;
        // TODO: there's no actual need to keep the TextRuns referenced
        // once they've been processed
        private ArrayBuilder<TextRun> _textRuns;
        private ArrayBuilder<GlyphRun> _glyphRuns;
        private ArrayBuilder<Line> _lines;
        private ArrayBuilder<PositionedGlyph> _glyphs;

        private uint _currentLineIdx;
        private float _prevBaselineY;
        private uint _lastWordStartGlyph;
        private float _lastWordStartGlyphX;
        private float _lastWordAscend;
        private float _lastWordDescend;
        private float _currentLineAscender;
        private float _currentLineDescender;
        private float _currentLineShift;

        private ArrayBuilder<RubyTextChunk> _rubyChunksOnLine;
        private uint _lastNonRubyGlyphOnLine;
        private char _lastAddedChar;

        private RectangleF _boundingBox;
        private float _bbLeft, _bbRight;

        public TextLayout(
            GlyphRasterizer glyphRasterizer,
            ReadOnlySpan<TextRun> textRuns,
            Size? maxBounds,
            float rubyFontSizeMultiplier = 0.4f)
        {
            _textRuns = new ArrayBuilder<TextRun>(textRuns.Length);
            _glyphs = new ArrayBuilder<PositionedGlyph>(initialCapacity: 32);
            _glyphRuns = new ArrayBuilder<GlyphRun>(initialCapacity: textRuns.Length);
            _lines = new ArrayBuilder<Line>(initialCapacity: 2);
            _rubyChunksOnLine = new ArrayBuilder<RubyTextChunk>(initialCapacity: 0);
            _maxBounds = maxBounds ?? new Size(uint.MaxValue, uint.MaxValue);
            _rubyFontSizeMultiplier = rubyFontSizeMultiplier;
            Clear();
            Append(glyphRasterizer, textRuns);
        }

        public ReadOnlySpan<GlyphRun> GlyphRuns => _glyphRuns.AsReadonlySpan();
        public ReadOnlySpan<PositionedGlyph> Glyphs => _glyphs.AsSpan();
        public RectangleF BoundingBox => _boundingBox;

        public void Clear()
        {
            _textRuns.Clear();
            _glyphRuns.Clear();
            _lines.Clear();
            _lines.Add(new Line());
            _glyphs.Clear();
            _currentLineIdx = 0;
            _prevBaselineY = 0;
            _lastWordStartGlyph = 0;
            _lastWordStartGlyphX = 0;
            _lastWordAscend = 0;
            _lastWordDescend = 0;
            _currentLineAscender = 0;
            _currentLineDescender = 0;
            _currentLineShift = 0;
            _rubyChunksOnLine.Clear();
            _lastNonRubyGlyphOnLine = 0;
            _lastAddedChar = default;
            _boundingBox = new RectangleF(x: 0, y: float.MaxValue, 0, 0);
            _bbLeft = float.MaxValue;
            _bbRight = 0;
        }

        public ReadOnlySpan<PositionedGlyph> GetGlyphs(GlyphSpan span)
        {
            return _glyphs.AsReadonlySpan((int)span.Start, (int)span.Length);
        }

        private Span<PositionedGlyph> GetGlyphsMut(GlyphSpan span)
        {
            return _glyphs.AsSpan((int)span.Start, (int)span.Length);
        }

        private ref Line CurrentLine => ref _lines[_currentLineIdx];

        public void Append(GlyphRasterizer glyphRasterizer, ReadOnlySpan<TextRun> textRuns)
        {
            for (uint i = 0; i < textRuns.Length; i++)
            {
                _textRuns.Add() = textRuns[(int)i];
                bool last = i == textRuns.Length - 1;
                if (!Append(glyphRasterizer, textRunIndex: _textRuns.Count - 1, last))
                {
                    break;
                }
            }
        }

        private bool Append(GlyphRasterizer glyphRasterizer, uint textRunIndex, bool lastTextRun)
        {
            TextRun textRun = _textRuns[(int)textRunIndex];
            uint glyphRunStart = _glyphs.Count;
            FontData fontData = glyphRasterizer.GetFontData(textRun.Font);
            ReadOnlySpan<char> text = textRun.Text.Span;
            uint nbNonWhitespaceOnLine = 0;
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char c = text[stringPos];
                uint glyphIndex = fontData.GetGlyphIndex(c);
                GlyphDimensions glyphDims = fontData.GetGlyphDimensions(glyphIndex, textRun.FontSize);
                bool isNewline = c == '\r' || c == '\n';
                bool isWhitespace = char.IsWhiteSpace(c);
                if (CanFitGlyph(glyphDims) || isNewline)
                {
                    var position = new Vector2(
                        _currentLineShift + CurrentLine.PenX + glyphDims.Left,
                        glyphDims.Top
                    );
                    if (position.X < 0)
                    {
                        _currentLineShift = -position.X;
                        position.X = 0;
                    }

                    _lastNonRubyGlyphOnLine = _glyphs.Count;
                    _glyphs.Add() = new PositionedGlyph(glyphIndex, position);
                    _lastWordAscend = MathF.Max(_lastWordAscend, glyphDims.Top);
                    _lastWordDescend = MathF.Max(_lastWordDescend, glyphDims.Height - glyphDims.Top + 1);
                    if (!isWhitespace)
                    {
                        nbNonWhitespaceOnLine++;
                    }
                    bool wordStart;
                    if (!textRun.HasRubyText)
                    {
                        wordStart = LineBreakingRules.CanStartLine(c);
                        if (_glyphs.Count > 1)
                        {
                            wordStart &= LineBreakingRules.CanEndLine(_lastAddedChar);
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
                        _lastWordStartGlyph = _glyphs.Count - 1;
                        _lastWordStartGlyphX = CurrentLine.PenX;
                        _lastWordAscend = 0;
                    }
                    if (CurrentLine.Length == 0 ||
                        LineBreakingRules.CanEndLine(c) && nbNonWhitespaceOnLine > 0)
                    {
                        if (!textRun.HasRubyText)
                        {
                            EndWord(textRun);
                        }
                    }

                    CurrentLine.Length++;
                    _lastAddedChar = c;
                    if (!isNewline)
                    {
                        CurrentLine.PenX += glyphDims.Advance;
                    }
                    else if (c == '\n')
                    {
                        Debug.Assert(!textRun.HasRubyText);
                        if (nbNonWhitespaceOnLine > 0)
                        {
                            EndWord(textRun);
                        }
                        nbNonWhitespaceOnLine = 0;
                        if (!StartNewLine(textRun, fontData, _glyphs.Count))
                        {
                            goto exit;
                        }
                    }
                }
                else
                {
                    uint lastWordLength = _glyphs.Count - _lastWordStartGlyph;
                    float lastWordWidth = CurrentLine.PenX - _lastWordStartGlyphX + 1;
                    CurrentLine.Length -= lastWordLength;
                    _lastNonRubyGlyphOnLine -= lastWordLength;
                    // Start a new line and move the last word to it
                    if (!StartNewLine(textRun, fontData, _lastWordStartGlyph))
                    {
                        _glyphs.Truncate(_glyphs.Count - lastWordLength);
                        goto exit;
                    }
                    nbNonWhitespaceOnLine = 0;

                    float basePosX = 0;
                    float shift = _glyphs[_lastWordStartGlyph].Position.X - _lastWordStartGlyphX;
                    if (shift < 0)
                    {
                        basePosX = -shift;
                    }
                    for (uint i = _lastWordStartGlyph; i < _glyphs.Count; i++)
                    {
                        ref PositionedGlyph g = ref _glyphs[i];
                        g = new PositionedGlyph(
                            g.Index,
                            new Vector2(basePosX + g.Position.X - _lastWordStartGlyphX, g.Position.Y)
                        );
                        CurrentLine.Length++;
                        nbNonWhitespaceOnLine++;
                    }

                    CurrentLine.PenX = lastWordWidth - 1;
                    // The one character that couldn't fit on the previous line
                    // and thus triggered a line break still needs to be positioned.
                    stringPos--;
                }
            }

        exit:
            if (_glyphs.Count == 0)
            {
                return false;
            }

            EndWord(textRun);
            var glyphSpan = GlyphSpan.FromBounds(
                start: glyphRunStart,
                end: _glyphs.Count
            );
            _glyphRuns.Add() = new GlyphRun(
                textRun.Font,
                textRun.FontSize,
                textRun.Color,
                textRun.OutlineColor,
                glyphSpan,
                isRuby: false
            );

            if (textRun.HasRubyText)
            {
                uint rubyTextLength = (uint)textRun.RubyText.Length;
                var rubyTextSpan = new GlyphSpan(
                    start: _glyphs.Count,
                    length: rubyTextLength
                );
                _glyphs.Append(count: rubyTextLength);
                CurrentLine.Length += rubyTextLength;
                _rubyChunksOnLine.Add(new RubyTextChunk
                {
                    RubyBaseSpan = glyphSpan,
                    RubyTextSpan = rubyTextSpan,
                    TextRun = textRunIndex
                });
                _glyphRuns.Add() = new GlyphRun(
                    textRun.Font,
                    GetRubyFontSize(textRun.FontSize),
                    textRun.Color,
                    textRun.OutlineColor,
                    rubyTextSpan,
                    isRuby: true
                );
            }

            uint lineLength = CurrentLine.Length;
            if (lastTextRun)
            {
                if (!FinishLine(textRun, fontData))
                {
                    if (textRun.HasRubyText)
                    {
                        _glyphRuns.RemoveLast();
                        _rubyChunksOnLine.RemoveLast();
                    }
                    ref GlyphRun lastRun = ref _glyphRuns.AsSpan()[^1];
                    uint lastRunLen = lastRun.GlyphSpan.Length;
                    lastRun = new GlyphRun(
                        lastRun.Font,
                        lastRun.FontSize,
                        lastRun.Color,
                        lastRun.OutlineColor,
                        new GlyphSpan(lastRun.GlyphSpan.Start, lastRunLen - lineLength),
                        isRuby: false
                    );
                    return false;
                }
            }

            return true;
        }

        private void EndWord(in TextRun textRun)
        {
            _currentLineAscender = MathF.Max(_currentLineAscender, _lastWordAscend);
            _currentLineDescender = MathF.Max(_currentLineDescender, _lastWordDescend);
            ref Line line = ref CurrentLine;
            line.LargestFontSize = textRun.FontSize.Value > line.LargestFontSize.Value
                ? textRun.FontSize
                : line.LargestFontSize;
        }

        private bool FinishLine(TextRun lastTextRun, FontData fontData)
        {
            ref Line line = ref CurrentLine;
            if (line.Length == 0) { return false; }
            Debug.Assert(!line.LargestFontSize.Equals(default));
            line.LargestFontMetrics = fontData.GetVerticalMetrics(line.LargestFontSize);
            if (!CalculateBaselineY(out float baselineY))
            {
                _glyphs.Truncate(_glyphs.Count - line.Length);
                line.Length = 0;
                return false;
            }

            float rubyTextBaselineOffset = 0, rubyTextAscend = 0;
            if (_rubyChunksOnLine.Count > 0)
            {
                LayOutRubyText(
                    fontData,
                    ref baselineY,
                    out rubyTextBaselineOffset,
                    out rubyTextAscend
                );
            }

            PositionedGlyph lastGlyph = _glyphs[_lastNonRubyGlyphOnLine];
            GlyphDimensions dims = fontData.GetGlyphDimensions(lastGlyph.Index, lastTextRun.FontSize);
            _bbRight = MathF.Max(_bbRight, lastGlyph.Position.X + dims.Width - 1);
            _bbLeft = MathF.Min(_bbLeft, _glyphs[line.Start].Position.X);

            float lineTop = _rubyChunksOnLine.Count == 0
                ? baselineY - _currentLineAscender + 1
                : baselineY + rubyTextBaselineOffset - rubyTextAscend + 1;
            float lineBottom = baselineY + _currentLineDescender - 1;
            float top = MathF.Min(_boundingBox.Y, lineTop);
            _boundingBox = new RectangleF(
                x: _bbLeft, y: top,
                width: _bbRight - _bbLeft + 1,
                height: MathF.Max(_boundingBox.Height, lineBottom - top + 1)
            );

            float? prevBaselineY = line.BaselineY;
            line.BaselineY = baselineY;
            if (prevBaselineY != null && baselineY != prevBaselineY)
            {
                Span<PositionedGlyph> oldGlyphs = _glyphs.AsSpan(
                    (int)line.Start,
                    (int)line.GlyphsPositioned
                );
                foreach (ref PositionedGlyph glyph in oldGlyphs)
                {
                    glyph = new PositionedGlyph(
                        glyph.Index,
                        new Vector2(
                            glyph.Position.X,
                            glyph.Position.Y + (baselineY - prevBaselineY.Value)
                        )
                    );
                }
            }

            uint nbNewGlyphs = line.Length - line.GlyphsPositioned;
            uint start = line.Start + line.GlyphsPositioned;
            Span<PositionedGlyph> newGlyphs = _glyphs.AsSpan((int)start, (int)nbNewGlyphs);
            foreach (ref PositionedGlyph glyph in newGlyphs)
            {
                glyph = new PositionedGlyph(
                    glyph.Index,
                    new Vector2(
                        glyph.Position.X,
                        baselineY - glyph.Position.Y
                    )
                );
            }
            line.GlyphsPositioned += nbNewGlyphs;
            return true;
        }

        private void LayOutRubyText(
            FontData fontData,
            ref float baselineY,
            out float rubyTextBaselineOffset,
            out float rubyTextAscend)
        {
            PtFontSize largestRubyFontSize = default;
            for (uint i = 0; i < _rubyChunksOnLine.Count; i++)
            {
                uint runIndex = _rubyChunksOnLine[i].TextRun;
                ref readonly TextRun rubyTextRun = ref _textRuns[runIndex];
                PtFontSize newFontSize = GetRubyFontSize(rubyTextRun.FontSize);
                largestRubyFontSize = newFontSize.Value > largestRubyFontSize.Value
                    ? newFontSize
                    : largestRubyFontSize;
            }

            VerticalMetrics largestRubyFontMetrics = fontData.GetVerticalMetrics(largestRubyFontSize);
            // Move the baseline down to fit the ruby text
            const float rubyTextMargin = 2.0f;
            baselineY += rubyTextMargin + largestRubyFontMetrics.Ascender;
            rubyTextBaselineOffset = -_currentLineAscender
                + largestRubyFontMetrics.Descender
                - rubyTextMargin;

            rubyTextAscend = 0;
            for (uint i = 0; i < _rubyChunksOnLine.Count; i++)
            {
                ref RubyTextChunk rtChunk = ref _rubyChunksOnLine[i];
                ReadOnlySpan<PositionedGlyph> rubyBaseGlyphs = GetGlyphs(rtChunk.RubyBaseSpan);
                Span<PositionedGlyph> rubyTextGlyphs = GetGlyphsMut(rtChunk.RubyTextSpan);
                ref readonly TextRun textRun = ref _textRuns[rtChunk.TextRun];
                PtFontSize rubyFontSize = GetRubyFontSize(textRun.FontSize);

                // Measure the base text
                ref readonly PositionedGlyph baseFirstGlyph = ref rubyBaseGlyphs[0];
                ref readonly PositionedGlyph baseLastGlyph = ref rubyBaseGlyphs[^1];
                float rubyBaseWidth = baseLastGlyph.Position.X - baseFirstGlyph.Position.X
                    + fontData.GetGlyphDimensions(baseLastGlyph.Index, textRun.FontSize).Width;

                // Measure the ruby text
                ReadOnlySpan<char> rubyText = textRun.RubyText.Span;
                Span<uint> glyphIndices = stackalloc uint[rubyText.Length];
                Span<GlyphDimensions> glyphDimensions = stackalloc GlyphDimensions[rubyText.Length];
                float rubyTextWidth = 0;
                const float rubyTextAdvance = 1.0f;

                for (int j = 0; j < rubyText.Length; j++)
                {
                    char c = rubyText[j];
                    uint glyphIndex = fontData.GetGlyphIndex(c);
                    glyphIndices[j] = glyphIndex;
                    GlyphDimensions dims = fontData.GetGlyphDimensions(glyphIndex, rubyFontSize);
                    glyphDimensions[j] = dims;
                    if (j < rubyText.Length - 1 || char.IsWhiteSpace(c))
                    {
                        rubyTextWidth += MathF.Round(dims.Advance + rubyTextAdvance);
                    }
                    rubyTextAscend = MathF.Max(rubyTextAscend, dims.Top);
                }

                // No need to position the glyphs if the chunk has alredy been processed before
                if (rtChunk.BeenProcessed) { continue; }

                if (rubyTextWidth <= rubyBaseWidth)
                {
                    // Space the ruby glyphs evenly if the base is long enough
                    float penX = rubyBaseGlyphs[0].Position.X;
                    float blockWidth = MathF.Floor(rubyBaseWidth / rubyText.Length);
                    float halfBlockWidth = MathF.Floor(blockWidth / 2.0f);
                    for (int j = 0; j < rubyText.Length; j++)
                    {
                        ref readonly GlyphDimensions glyphDims = ref glyphDimensions[j];
                        var position = new Vector2(
                            penX + halfBlockWidth - MathF.Round(glyphDims.Width / 2.0f),
                            -(rubyTextBaselineOffset - glyphDims.Top)
                        );
                        rubyTextGlyphs[j] = new PositionedGlyph(glyphIndices[j], position);
                        penX += blockWidth;
                    }
                }
                else
                {
                    // Otherwise, center the ruby text over the base
                    float penX = rubyBaseGlyphs[0].Position.X
                        - MathF.Round((rubyTextWidth - rubyBaseWidth) / 2.0f);
                    if (penX < 0.0f)
                    {
                        penX = rubyBaseGlyphs[0].Position.X;
                    }

                    for (int j = 0; j < rubyText.Length; j++)
                    {
                        ref readonly GlyphDimensions glyphDims = ref glyphDimensions[j];
                        var position = new Vector2(
                            penX + glyphDims.Left,
                            -(rubyTextBaselineOffset - glyphDims.Top)
                        );
                        rubyTextGlyphs[j] = new PositionedGlyph(glyphIndices[j], position);
                        penX += MathF.Round(glyphDims.Advance + rubyTextAdvance);
                        if (penX > _maxBounds.Width)
                        {
                            break;
                        }
                    }
                }
                rtChunk.BeenProcessed = true;
            }
        }

        private PtFontSize GetRubyFontSize(PtFontSize regularFontSize)
        {
            int size = (int)MathF.Round(regularFontSize.ToFloat() * _rubyFontSizeMultiplier);
            return new PtFontSize(size);
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

        public bool StartNewLine(TextRun lastTextRun, FontData fontData, uint startGlyph, float baselineX = 0)
        {
            if (FinishLine(lastTextRun, fontData))
            {
                _prevBaselineY = CurrentLine.BaselineY!.Value;
                _lines.Add(new Line());
                _currentLineIdx++;
                CurrentLine.Start = startGlyph;
                CurrentLine.PenX = baselineX;
                _rubyChunksOnLine.Clear();
                _currentLineAscender = 0;
                _currentLineDescender = 0;
                _lastNonRubyGlyphOnLine = 0;
                _currentLineShift = 0;
                return true;
            }

            return false;
        }

        private bool CanFitGlyph(in GlyphDimensions glyphDims)
        {
            return (CurrentLine.PenX
                + glyphDims.Width
                + glyphDims.Left) <= _maxBounds.Width;
        }
    }
}
