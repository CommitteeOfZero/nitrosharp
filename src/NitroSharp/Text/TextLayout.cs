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
        public readonly FontFaceKey Font;
        public readonly PtFontSize FontSize;
        public readonly RgbaFloat Color;
        public readonly RgbaFloat OutlineColor;
        public readonly GlyphSpan GlyphSpan;
        public readonly bool DrawOutline;

        public GlyphRun(
            FontFaceKey font, PtFontSize fontSize,
            RgbaFloat color, RgbaFloat outlineColor,
            GlyphSpan glyphSpan, bool drawOutline)
        {
            Font = font;
            FontSize = fontSize;
            Color = color;
            OutlineColor = outlineColor;
            GlyphSpan = glyphSpan;
            DrawOutline = drawOutline;
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
        private readonly struct Line
        {
            public readonly uint Start;
            public readonly uint Length;
            public readonly float BaselineY;
            public readonly float MaxFontDescender;
            public readonly float MaxFontLineGap;

            public Line(
                uint start,
                uint length,
                float baselineY,
                float maxFontDescender,
                float maxFontLineGap)
            {
                Start = start;
                Length = length;
                BaselineY = baselineY;
                MaxFontDescender = maxFontDescender;
                MaxFontLineGap = maxFontLineGap;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct LineBuilder
        {
            private ArrayBuilder<RubyTextChunk> _rubyChunks;

            public float BaselineY;
            public float Shift;
            public uint GlyphsPositioned;

            public uint Start { get; private set; }
            public uint Length { get; private set; }
            public float LastPenX { get; private set; }

            public float ActualAscender { get; private set; }
            public float ActualDescender { get; private set; }

            public float MaxFontAscender { get; private set; }
            public float MaxFontDescender { get; private set; }
            public float MaxFontLineGap { get; private set; }

            public float Left { get; private set; }
            public float Right { get; private set; }

            public Span<RubyTextChunk> RubyChunks => _rubyChunks.AsSpan();

            public static LineBuilder Create()
            {
                return new LineBuilder
                {
                    _rubyChunks = new ArrayBuilder<RubyTextChunk>(initialCapacity: 0),
                    Left = float.MaxValue
                };
            }

            public void AddRubyChunk(in RubyTextChunk chunk)
            {
                _rubyChunks.Add(chunk);
                Length += chunk.RubyTextSpan.Length;
            }

            public void Reset(uint start)
            {
                ArrayBuilder<RubyTextChunk> rubyChunks = _rubyChunks;
                this = default;
                Start = start;
                Left = float.MaxValue;
                rubyChunks.Clear();
                _rubyChunks = rubyChunks;
            }

            public void AppendWord(ref Word word, in VerticalMetrics fontMetrics)
            {
                Length += word.Length;
                LastPenX += word.PenX;
                ActualAscender = MathF.Max(ActualAscender, word.Ascent);
                ActualDescender = MathF.Max(ActualDescender, word.Descent);
                MaxFontAscender = MathF.Max(MaxFontAscender, fontMetrics.Ascender);
                MaxFontDescender = MathF.Min(MaxFontDescender, fontMetrics.Descender);
                MaxFontLineGap = MathF.Min(MaxFontLineGap, fontMetrics.LineGap);
                if (word.Length > 0)
                {
                    Left = MathF.Min(Left, word.Left);
                    Right = word.Right;
                }

                word = new Word(word.Start + word.Length);
            }

            public Line Build()
            {
                return new Line(
                    Start,
                    Length,
                    BaselineY,
                    MaxFontDescender,
                    MaxFontLineGap
                );
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct Word
        {
            public Word(uint start) : this()
            {
                Start = start;
            }

            public float PenX { get; private set; }
            public uint Start { get; }
            public uint Length { get; private set; }
            public float Left { get; private set; }
            public float Right { get; private set; }
            public float Ascent { get; private set; }
            public float Descent { get; private set; }
            public bool HasNonWhitespaceChars { get; private set; }

            public void Append(in GlyphDimensions glyph, Vector2 position, bool isWhitespace)
            {
                PenX += glyph.Advance;
                Ascent = MathF.Max(Ascent, glyph.Top);
                Descent = MathF.Max(Descent, glyph.Height - glyph.Top - 1);
                if (Length == 0)
                {
                    Left = position.X;
                }
                float w = glyph.Width > 0 ? glyph.Width : glyph.Advance;
                Right = position.X + w - 1;
                Length++;
                HasNonWhitespaceChars |= !isWhitespace;
            }
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
        private readonly float? _fixedLineHeight;

        private readonly float _rubyFontSizeMultiplier;
        // TODO: there's no actual need to keep the TextRuns referenced
        // once they've been processed
        private ArrayBuilder<TextRun> _textRuns;
        private ArrayBuilder<GlyphRun> _glyphRuns;
        private ArrayBuilder<Line> _lines;
        private ArrayBuilder<PositionedGlyph> _glyphs;

        private Word _lastWord;
        private uint _currentLineIdx;
        private LineBuilder _lineBuilder;

        private float _prevBaselineY;

        private RectangleF _boundingBox;
        private float _bbLeft, _bbRight;

        public TextLayout(
            Size? maxBounds = null,
            float? fixedLineHeight = null,
            float rubyFontSizeMultiplier = 0.4f)
        {
            _textRuns = new ArrayBuilder<TextRun>(initialCapacity: 2);
            _glyphs = new ArrayBuilder<PositionedGlyph>(initialCapacity: 32);
            _glyphRuns = new ArrayBuilder<GlyphRun>(initialCapacity: 2);
            _lines = new ArrayBuilder<Line>(initialCapacity: 2);
            _maxBounds = maxBounds ?? new Size(uint.MaxValue, uint.MaxValue);
            _fixedLineHeight = fixedLineHeight;
            _rubyFontSizeMultiplier = rubyFontSizeMultiplier;
            _lineBuilder = LineBuilder.Create();
            Clear();
        }

        public TextLayout(
            GlyphRasterizer glyphRasterizer,
            ReadOnlySpan<TextRun> textRuns,
            Size? maxBounds,
            float? fixedLineHeight = null,
            float rubyFontSizeMultiplier = 0.4f)
            : this(maxBounds, fixedLineHeight, rubyFontSizeMultiplier)
        {
            Append(glyphRasterizer, textRuns);
        }

        private float PenX => _lineBuilder.LastPenX + _lastWord.PenX;

        public ReadOnlySpan<GlyphRun> GlyphRuns => _glyphRuns.AsReadonlySpan();
        public ReadOnlySpan<PositionedGlyph> Glyphs => _glyphs.AsSpan();
        public RectangleF BoundingBox => _boundingBox;
        public Size MaxBounds => _maxBounds;

        public void Clear()
        {
            _textRuns.Clear();
            _glyphRuns.Clear();
            _lines.Clear();
            _lines.Add(new Line());
            _glyphs.Clear();
            _currentLineIdx = 0;
            _prevBaselineY = 0;
            _lastWord = default;
            _lineBuilder.Reset(start: 0);
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
             bool canFitGlyph(in GlyphDimensions glyphDims)
            {
                return (PenX
                    + glyphDims.Width
                    + glyphDims.Left) <= _maxBounds.Width;
            }

            TextRun textRun = _textRuns[(int)textRunIndex];
            uint glyphRunStart = _glyphs.Count;
            FontData fontData = glyphRasterizer.GetFontData(textRun.Font);
            VerticalMetrics fontMetrics = fontData.GetVerticalMetrics(textRun.FontSize);
            ReadOnlySpan<char> text = textRun.Text.Span;
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                bool end = stringPos == text.Length - 1;
                char c = text[stringPos];
                uint glyphIndex = fontData.GetGlyphIndex(c);
                GlyphDimensions glyphDims = fontData.GetGlyphDimensions(glyphIndex, textRun.FontSize);
                bool isNewline = c == '\r' || c == '\n';
                // Disallow line breaking if the TextRun has ruby text
                bool mayBreakLine = !(textRun.HasRubyText && stringPos > 0);
                if (canFitGlyph(glyphDims))
                {
                    if (!isNewline)
                    {
                        var position = new Vector2(
                            _lineBuilder.Shift + PenX + glyphDims.Left,
                            glyphDims.Top
                        );
                        if (position.X < 0)
                        {
                            _lineBuilder.Shift = -position.X;
                            position.X = 0;
                        }

                        _glyphs.Add() = new PositionedGlyph(glyphIndex, position);
                        _lastWord.Append(glyphDims, position, char.IsWhiteSpace(c));
                        
                    }

                    if (mayBreakLine && LineBreakingRules.CanEndLine(c) && _lastWord.HasNonWhitespaceChars
                        && (stringPos == text.Length - 1 || LineBreakingRules.CanStartLine(text[stringPos + 1])))
                    {
                        if (!textRun.HasRubyText)
                        {
                            _lineBuilder.AppendWord(ref _lastWord, fontMetrics);
                        }
                    }

                    if (c == '\n' && !StartNewLine(glyphRasterizer, _glyphs.Count))
                    {
                        goto exit;
                    }
                }
                else
                {
                    if (mayBreakLine && LineBreakingRules.CanStartLine(c)
                        && LineBreakingRules.CanEndLine(text[stringPos - 1]))
                    {
                        if (!StartNewLine(glyphRasterizer, _glyphs.Count))
                        {
                            goto exit;
                        }

                        stringPos--;
                        continue;
                    }

                    _lastWord = new Word(_lastWord.Start);
                    // Start a new line and move the last word to it
                    if (!StartNewLine(glyphRasterizer, _lastWord.Start))
                    {
                        _glyphs.Truncate(_glyphs.Count - _lastWord.Length);
                        goto exit;
                    }

                    for (uint i = _lastWord.Start; i < _glyphs.Count; i++)
                    {
                        ref PositionedGlyph g = ref _glyphs[i];
                        GlyphDimensions dims = fontData.GetGlyphDimensions(g.Index, textRun.FontSize);
                        var pos = new Vector2(
                            PenX + dims.Left,
                            dims.Top
                        );
                        g = new PositionedGlyph(g.Index, pos);
                        _lastWord.Append(dims, pos, isWhitespace: false);
                    }

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

            _lineBuilder.AppendWord(ref _lastWord, fontMetrics);
            Debug.Assert(_glyphs.Count >= glyphRunStart);
            var glyphSpan = GlyphSpan.FromBounds(
                start: glyphRunStart,
                end: _glyphs.Count
            );
            if (glyphSpan.IsEmpty)
            {
                return true;
            }

            _glyphRuns.Add() = new GlyphRun(
                textRun.Font,
                textRun.FontSize,
                textRun.Color,
                textRun.OutlineColor,
                glyphSpan,
                textRun.DrawOutline
            );

            if (textRun.HasRubyText)
            {
                uint rubyTextLength = (uint)textRun.RubyText.Length;
                var rubyTextSpan = new GlyphSpan(
                    start: _glyphs.Count,
                    length: rubyTextLength
                );
                _glyphs.Append(count: rubyTextLength);
                _lineBuilder.AddRubyChunk(new RubyTextChunk
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
                    textRun.DrawOutline
                );
            }

            bool enoughSpace = lastTextRun
                ? ProcessLine(glyphRasterizer)
                : CalculateBaselineY(out _);
            if (!enoughSpace)
            {
                if (textRun.HasRubyText)
                {
                    _glyphRuns.RemoveLast();
                }
                ref GlyphRun lastRun = ref _glyphRuns.AsSpan()[^1];
                uint lastRunLen = lastRun.GlyphSpan.Length;
                Debug.Assert(lastRunLen >= _lineBuilder.Length);
                uint newLength = lastRunLen - _lineBuilder.Length;
                if (newLength > 0)
                {
                    lastRun = new GlyphRun(
                        lastRun.Font,
                        lastRun.FontSize,
                        lastRun.Color,
                        lastRun.OutlineColor,
                        new GlyphSpan(lastRun.GlyphSpan.Start, newLength),
                        textRun.DrawOutline
                    );
                }
                else
                {
                    _glyphRuns.RemoveLast();
                }

                _glyphs.Truncate(_glyphs.Count - _lineBuilder.Length);
                _lineBuilder.Reset(_glyphs.Count);
                _lastWord = new Word(_glyphs.Count);
                return false;
            }

            return true;
        }

        private bool ProcessLine(GlyphRasterizer glyphRasterizer)
        {
            ref LineBuilder lineBuilder = ref _lineBuilder;
            if (lineBuilder.Length == 0 || !CalculateBaselineY(out float baselineY))
            {
                return false;
            }

            float rubyTextBaselineOffset = 0, rubyTextAscend = 0;
            if (lineBuilder.RubyChunks.Length > 0)
            {
                LayOutRubyText(
                    glyphRasterizer,
                    ref baselineY,
                    out rubyTextBaselineOffset,
                    out rubyTextAscend
                );
            }

            _bbRight = MathF.Max(_bbRight, lineBuilder.Right);
            _bbLeft = MathF.Min(_bbLeft, lineBuilder.Left);

            float lineTop = lineBuilder.RubyChunks.Length == 0
                ? baselineY - lineBuilder.ActualAscender
                : baselineY + rubyTextBaselineOffset - rubyTextAscend;
            float lineBottomActual = baselineY + lineBuilder.ActualDescender;
            float lineBottom = _fixedLineHeight is float height && height >= (lineBottomActual - lineTop)
                ? lineTop + height - 1
                : lineBottomActual;
            float top = MathF.Min(_boundingBox.Y, lineTop);
            _boundingBox = new RectangleF(
                x: _bbLeft, y: top,
                width: _bbRight - _bbLeft + 1,
                height: MathF.Max(_boundingBox.Height, lineBottom - top + 1)
            );

            float? prevBaselineY = lineBuilder.BaselineY;
            lineBuilder.BaselineY = baselineY;
            if (prevBaselineY != null && baselineY != prevBaselineY)
            {
                Span<PositionedGlyph> oldGlyphs = _glyphs.AsSpan(
                    (int)lineBuilder.Start,
                    (int)lineBuilder.GlyphsPositioned
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

            uint nbNewGlyphs = lineBuilder.Length - lineBuilder.GlyphsPositioned;
            uint start = lineBuilder.Start + lineBuilder.GlyphsPositioned;
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
            lineBuilder.GlyphsPositioned += nbNewGlyphs;
            _lines[^1] = _lineBuilder.Build();
            return true;
        }

        private void LayOutRubyText(
            GlyphRasterizer glyphRasterizer,
            ref float baselineY,
            out float rubyTextBaselineOffset,
            out float rubyTextAscend)
        {
            ref LineBuilder line = ref _lineBuilder;
            Span<RubyTextChunk> rubyChunks = line.RubyChunks;

            float maxRubyAscender = 0, maxRubyDescender = 0;
            for (int i = 0; i < rubyChunks.Length; i++)
            {
                uint runIndex = rubyChunks[i].TextRun;
                ref readonly TextRun rubyTextRun = ref _textRuns[runIndex];
                FontData fontData = glyphRasterizer.GetFontData(rubyTextRun.Font);
                PtFontSize fontSize = GetRubyFontSize(rubyTextRun.FontSize);
                VerticalMetrics metrics = fontData.GetVerticalMetrics(fontSize);
                maxRubyAscender = MathF.Max(maxRubyAscender, metrics.Ascender);
                maxRubyDescender = MathF.Max(maxRubyDescender, metrics.Descender);
            }

            // Move the baseline down to fit the ruby text
            const float rubyTextMargin = 2.0f;
            baselineY += rubyTextMargin + maxRubyAscender;
            rubyTextBaselineOffset = -line.ActualAscender + maxRubyDescender - rubyTextMargin;

            rubyTextAscend = 0;
            for (int i = 0; i < rubyChunks.Length; i++)
            {
                ref RubyTextChunk rtChunk = ref rubyChunks[i];
                ReadOnlySpan<PositionedGlyph> rubyBaseGlyphs = GetGlyphs(rtChunk.RubyBaseSpan);
                Span<PositionedGlyph> rubyTextGlyphs = GetGlyphsMut(rtChunk.RubyTextSpan);
                ref readonly TextRun textRun = ref _textRuns[rtChunk.TextRun];
                FontData fontData = glyphRasterizer.GetFontData(textRun.Font);
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
            if (_currentLineIdx > 0)
            {
                const float minLineGap = 2.0f;
                float minHeight = _lineBuilder.ActualDescender
                    + _lineBuilder.ActualAscender + minLineGap;

                if (_fixedLineHeight is {} lineHeight)
                {
                    baselineY = _prevBaselineY + Math.Max(minHeight, lineHeight);
                }
                else
                {
                    ref Line prevLine = ref _lines[_currentLineIdx - 1];
                    baselineY = _prevBaselineY
                        + _lineBuilder.MaxFontAscender
                        - prevLine.MaxFontDescender
                        + prevLine.MaxFontLineGap;
                }
            }
            else
            {
                baselineY = _prevBaselineY + _lineBuilder.MaxFontAscender;
            }

            return (baselineY + Math.Abs(_lineBuilder.MaxFontDescender))
                <= _maxBounds.Height;
        }

        public bool StartNewLine(GlyphRasterizer glyphRasterizer, uint startGlyph)
        {
            if (ProcessLine(glyphRasterizer))
            {
                _prevBaselineY = _lineBuilder.BaselineY;
                _lineBuilder.Reset(startGlyph);
                _currentLineIdx++;
                _lines.Add();
                return true;
            }

            return false;
        }
    }
}
