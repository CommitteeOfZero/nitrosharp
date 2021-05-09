using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;

// Inspired by https://github.com/alexheretic/glyph-brush
namespace NitroSharp.Text
{
    internal sealed class TextLayout
    {
        private readonly float? _fixedLineHeight;
        private readonly float _rubyFontSizeMultiplier;
        private Vector2 _caret = Vector2.Zero;
        private RectangleF _boundingBox;

        private readonly List<Line> _lines = new();
        private readonly List<GlyphRun> _glyphRuns = new();
        private readonly List<PositionedGlyph> _glyphs = new();
        private readonly List<float> _opacityValues = new();

        public TextLayout(
            uint? maxWidth = null,
            uint? maxHeight = null,
            float? fixedLineHeight = null,
            float rubyFontSizeMultiplier = 0.4f)
        {
            uint mWidth = maxWidth ?? uint.MaxValue;
            uint mHeight = maxHeight ?? uint.MaxValue;
            MaxBounds = new Size(mWidth, mHeight);
            _fixedLineHeight = fixedLineHeight;
            _rubyFontSizeMultiplier = rubyFontSizeMultiplier;
            Clear();
        }

        public TextLayout(
            GlyphRasterizer glyphRasterizer,
            ReadOnlySpan<TextRun> textRuns,
            uint? maxWidth = null,
            uint? maxHeight = null,
            float? fixedLineHeight = null,
            float rubyFontSizeMultiplier = 0.4f)
            : this(maxWidth, maxHeight, fixedLineHeight, rubyFontSizeMultiplier)
        {
            Append(glyphRasterizer, textRuns);
        }

        public ReadOnlySpan<GlyphRun> GlyphRuns => CollectionsMarshal.AsSpan(_glyphRuns);
        public ReadOnlySpan<PositionedGlyph> Glyphs => CollectionsMarshal.AsSpan(_glyphs);
        public ReadOnlySpan<float> OpacityValues => CollectionsMarshal.AsSpan(_opacityValues);
        public ReadOnlySpan<Line> Lines => CollectionsMarshal.AsSpan(_lines);
        public RectangleF BoundingBox => _boundingBox;
        public Size MaxBounds { get; }

        public int GetGlyphSpanLength(Range span)
            => span.GetOffsetAndLength(_glyphs.Count).Length;

        public ReadOnlySpan<float> GetOpacityValues(Range span)
            => CollectionsMarshal.AsSpan(_opacityValues)[span];

        public Span<float> GetOpacityValuesMut(Range span)
            => CollectionsMarshal.AsSpan(_opacityValues)[span];

        public void Clear()
        {
            _glyphRuns.Clear();
            _opacityValues.Clear();
            _glyphs.Clear();
            _lines.Clear();
            _caret = Vector2.Zero;
            _boundingBox = RectangleF.FromLTRB(
                float.MaxValue, float.MaxValue,
                float.MinValue, float.MinValue
            );
        }

        public void Append(GlyphRasterizer glyphRasterizer, TextRun textRun)
            => Append(glyphRasterizer, MemoryMarshal.CreateReadOnlySpan(ref textRun, 1));

        public void Append(GlyphRasterizer glyphRasterizer, ReadOnlySpan<TextRun> textRuns)
        {
            bool updateLastLine = _glyphRuns.Count > 0;
            int appendStart = _glyphs.Count;
            var glyphBuf = new List<TextRunGlyph>();
            var context = new TextLayoutContext
            {
                GlyphRasterizer = glyphRasterizer,
                MaxBounds = MaxBounds,
                RubyFontSizeMultiplier = _rubyFontSizeMultiplier,
                GlyphBuffer = glyphBuf
            };

            var lines = new LineEnumerable(context, textRuns, _caret.X);
            int runStart = 0, runLength = 0;
            int pos = 0;
            uint lastRunIndex = 0;
            CharacterKind lastCharKind = CharacterKind.Regular;
            GlyphRun? lastRun = default;
            float left = _boundingBox.Left;
            float right = _boundingBox.Right;
            float top = _boundingBox.Top;
            float bottom = _boundingBox.Bottom;
            Line prevLine = new();
            foreach (Line line in lines)
            {
                float height = _fixedLineHeight ?? line.VerticalMetrics.LineHeight;
                float newY = _caret.Y;
                if (!updateLastLine && prevLine.VerticalMetrics.LineHeight > 0)
                {
                    newY += _fixedLineHeight ?? prevLine.VerticalMetrics.LineHeight;
                }
                if (newY + height > MaxBounds.Height) { return; }
                _caret.Y = newY;

                Span<TextRunGlyph> glyphs = CollectionsMarshal.AsSpan(glyphBuf);
                foreach (TextRunGlyph glyph in glyphs[line.GlyphSpan])
                {
                    var glyphPos = new Vector2(glyph.Position.X, _caret.Y + glyph.Position.Y);
                    if (glyph.Kind == CharacterKind.RubyText)
                    {
                        glyphPos.Y -= line.VerticalMetrics.Ascender + 3;
                    }
                    _glyphs.Add(new PositionedGlyph(glyph.GlyphIndex, glyph.IsWhitespace, glyphPos));
                    _opacityValues.Add(1.0f);

                    bool endGlyphRun = pos > 0
                        && (glyph.TextRunIndex != lastRunIndex || glyph.Kind != lastCharKind);
                    if (!endGlyphRun)
                    {
                        runLength++;
                    }
                    if (endGlyphRun || pos == line.GlyphSpan.End.Value - 1)
                    {
                        (int runIndex, CharacterKind kind) = endGlyphRun
                            ? ((int)lastRunIndex, lastCharKind)
                            : ((int)glyph.TextRunIndex, glyph.Kind);
                        ref readonly TextRun textRun = ref textRuns[runIndex];
                        PtFontSize fontSize = kind == CharacterKind.RubyText
                            ? new PtFontSize((int)(textRun.FontSize.Value * _rubyFontSizeMultiplier))
                            : textRun.FontSize;

                        GlyphRunFlags flags = textRun.DrawOutline
                            ? GlyphRunFlags.Outline
                            : GlyphRunFlags.None;
                        if (lastCharKind == CharacterKind.RubyBase)
                        {
                            flags |= GlyphRunFlags.RubyBase;
                        }
                        else if (lastCharKind == CharacterKind.RubyText)
                        {
                            flags |= GlyphRunFlags.RubyText;
                            flags &= ~GlyphRunFlags.Outline;
                        }

                        if (lastRun is null)
                        {
                            lastRun = new GlyphRun
                            {
                                Font = textRun.Font,
                                FontSize = fontSize,
                                Color = textRun.Color,
                                OutlineColor = textRun.OutlineColor,
                                Flags = flags,
                                GlyphSpan = new Range(runStart, runStart + runLength)
                            };
                        }
                        else
                        {
                            lastRun = lastRun.Value
                                .WithSpan(new Range(runStart, runStart + runLength));
                        }

                        if (endGlyphRun)
                        {
                            AppendGlyphRun(lastRun.Value, appendStart);
                            runStart = pos;
                            runLength = 1;
                            lastRun = null;
                        }
                    }

                    pos++;
                    lastCharKind = glyph.Kind;
                    lastRunIndex = glyph.TextRunIndex;
                    _caret.X = line.Right;
                }

                var actualLineSpan = new Range(
                    line.GlyphSpan.Start.Value + appendStart,
                    line.GlyphSpan.End.Value + appendStart
                );

                if (updateLastLine)
                {
                    Line lastLine = _lines[^1];
                    var newSpan = new Range(lastLine.GlyphSpan.Start, actualLineSpan.End);
                    _lines[^1] = new Line
                    {
                        GlyphSpan = newSpan,
                        BbLeft = lastLine.BbLeft,
                        BbRight = line.BbRight,
                        BaselineY = line.BaselineY,
                        VerticalMetrics = line.VerticalMetrics,
                        ActualAscender = line.ActualAscender,
                        ActualDescender = line.ActualDescender
                    };
                }
                else
                {
                    _lines.Add(line.WithSpan(actualLineSpan));
                }

                if (!line.IsEmpty)
                {
                    bottom = _caret.Y + (_fixedLineHeight ?? (line.BaselineY + line.ActualDescender + 1));
                    left = MathF.Min(left, line.BbLeft);
                    right = MathF.Max(right, line.BbRight);
                    top = MathF.Min(top, _caret.Y + line.BaselineY - line.ActualAscender);
                }
                else
                {
                    _caret.X = 0;
                }
                updateLastLine = false;
                prevLine = line;
            }

            if (lastRun.HasValue)
            {
                AppendGlyphRun(lastRun.Value, appendStart);
            }

            _boundingBox = RectangleF.FromLTRB(left, top, right, bottom);
        }

        private void AppendGlyphRun(in GlyphRun run, int appendStart)
        {
            var actualSpan = new Range(
                run.GlyphSpan.Start.Value + appendStart,
                run.GlyphSpan.End.Value + appendStart
            );
            if (_glyphRuns.Count > 0)
            {
                Debug.Assert(actualSpan.Start.Value == _glyphRuns[^1].GlyphSpan.End.Value);
            }
            _glyphRuns.Add(run.WithSpan(actualSpan));
        }

        public void NewLine()
        {
            if (_lines.Count > 0)
            {
                float newY = _caret.Y + (_fixedLineHeight ?? _lines[^1].VerticalMetrics.LineHeight);
                if (newY < MaxBounds.Height)
                {
                    _caret = new Vector2(0, newY);
                }

                _lines.Add(new Line
                {
                    GlyphSpan = new Range(_glyphs.Count, _glyphs.Count),
                    BbLeft = float.MaxValue
                });
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct PositionedGlyph
    {
        public readonly uint Index;
        private readonly GlyphFlags Flags;
        public readonly Vector2 Position;

        private PositionedGlyph(uint index, GlyphFlags flags, Vector2 position)
        {
            Index = index;
            Flags = flags;
            Position = position;
        }

        public PositionedGlyph(uint index, bool isWhitespace, Vector2 position)
            : this(index, isWhitespace ? GlyphFlags.Whitespace : GlyphFlags.None, position)
        {
        }

        public bool IsWhitespace => (Flags & GlyphFlags.Whitespace) == GlyphFlags.Whitespace;

        public override int GetHashCode() => HashCode.Combine(Index, Position);
        public override string ToString() => $"{{Glyph #{Index}, {Position}}}";
    }

    [Flags]
    internal enum GlyphFlags
    {
        None = 0,
        Whitespace = 1
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct GlyphRun
    {
        public FontFaceKey Font { get; init; }
        public PtFontSize FontSize { get; init; }
        public RgbaFloat Color { get; init; }
        public RgbaFloat OutlineColor { get; init; }
        public Range GlyphSpan { get; init; }
        public GlyphRunFlags Flags { get; init; }

        public bool DrawOutline => (Flags & GlyphRunFlags.Outline) == GlyphRunFlags.Outline;
        public bool IsRubyBase => (Flags & GlyphRunFlags.RubyBase) == GlyphRunFlags.RubyBase;
        public bool IsRubyText => (Flags & GlyphRunFlags.RubyText) == GlyphRunFlags.RubyText;

        public GlyphRun WithSpan(Range span) => new()
        {
            Font = Font,
            FontSize = FontSize,
            Color = Color,
            OutlineColor = OutlineColor,
            GlyphSpan = span,
            Flags = Flags
        };
    }

    [Flags]
    internal enum GlyphRunFlags
    {
        None = 0,
        Outline = 1,
        RubyBase = 2,
        RubyText = 4
    }

    internal readonly struct TextLayoutContext
    {
        public GlyphRasterizer GlyphRasterizer { get; init; }
        public Size MaxBounds { get; init; }
        public float RubyFontSizeMultiplier { get; init; }
        public List<TextRunGlyph> GlyphBuffer { get; init; }
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct Line
    {
        public Range GlyphSpan { get; init; }
        public VerticalMetrics VerticalMetrics { get; init; }
        public float BaselineY { get; init; }
        public float Right { get; init; }
        public float BbLeft { get; init; }
        public float BbRight { get; init; }
        public float ActualAscender { get; init; }
        public float ActualDescender { get; init; }

        public bool IsEmpty => GlyphSpan.End.Value == GlyphSpan.Start.Value;

        public Line WithSpan(Range span) => new()
        {
            GlyphSpan = span,
            VerticalMetrics = VerticalMetrics,
            BaselineY = BaselineY,
            BbLeft = BbLeft,
            BbRight = BbRight,
            ActualAscender = ActualAscender,
            ActualDescender = ActualDescender
        };
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct Word
    {
        public Range GlyphRange { get; init; }
        public float BbLeft { get; init; }
        public float BbRight { get; init; }
        public float AdvanceWidth { get; init; }
        public float AdvanceWidthNoTrail { get; init; }
        public VerticalMetrics MaxVMetrics { get; init; }
        public bool HardBreak { get; init; }
        public float ActualAscender { get; init; }
        public float ActualDescender { get; init; }

        public int Length => GlyphRange.End.Value - GlyphRange.Start.Value;

        public Word WithRange(Range glyphRange) => new()
        {
            GlyphRange = glyphRange,
            BbLeft = BbLeft,
            BbRight = BbRight,
            AdvanceWidth = AdvanceWidth,
            AdvanceWidthNoTrail = AdvanceWidthNoTrail,
            MaxVMetrics = MaxVMetrics,
            HardBreak = HardBreak,
            ActualAscender = ActualAscender,
            ActualDescender = ActualDescender
        };
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct Character
    {
        public CharacterKind Kind { get; init; }
        public Rune Scalar { get; init; }
        public uint TextRun { get; init; }
        public FontData Font { get; init; }
        public PtFontSize FontSize { get; init; }
        public uint GlyphIndex { get; init; }
        public LineBreak? LineBreak { get; init; }
    }

    internal enum CharacterKind
    {
        Regular,
        RubyBase,
        RubyText
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct TextRunGlyph
    {
        public CharacterKind Kind { get; init; }
        public uint TextRunIndex { get; init; }
        public uint GlyphIndex { get; init; }
        public Vector2 Position;
        public bool IsWhitespace { get; init; }
    }

    internal readonly ref struct LineEnumerable
    {
        private readonly TextLayoutContext _context;
        private readonly ReadOnlySpan<TextRun> _textRuns;
        private readonly float _caretStartX;

        public LineEnumerable(
            in TextLayoutContext context,
            ReadOnlySpan<TextRun> textRuns,
            float caretStartX)
        {
            _context = context;
            _textRuns = textRuns;
            _caretStartX = caretStartX;
        }

        public LineEnumerator GetEnumerator() => new(_context, _textRuns, _caretStartX);
    }

    internal ref struct LineEnumerator
    {
        private readonly TextLayoutContext _context;
        private readonly float _caretStartX;
        private WordEnumerator _words;
        private Word? _peekedWord;
        private bool _firstLine;

        public LineEnumerator(
            in TextLayoutContext context,
            ReadOnlySpan<TextRun> textRuns,
            float caretStartX)
        {
            _context = context;
            _caretStartX = caretStartX;
            _words = new WordEnumerator(context, textRuns);
            _peekedWord = null;
            _firstLine = true;
            Current = default;
        }

        public Line Current { get; private set; }

        public bool MoveNext()
        {
            Vector2 caret = _firstLine ? new Vector2(_caretStartX, 0) : Vector2.Zero;
            int start = int.MaxValue, end = int.MinValue;
            float ascender = 0, descender = 0;
            float bbLeft = float.MaxValue;
            float bbRight = 0;
            VerticalMetrics maxVMetrics = new();
            bool notEmpty = false;
            while (_peekedWord is { } || _words.MoveNext())
            {
                Word word = _peekedWord ?? _words.Current;
                bool firstWord = !notEmpty;
                _peekedWord = null;
                float right = caret.X + word.AdvanceWidthNoTrail;
                if (right > _context.MaxBounds.Width)
                {
                    _peekedWord = word;
                    break;
                }

                notEmpty = true;
                if ((firstWord || word.Length > 0)
                    && word.MaxVMetrics.LineHeight > maxVMetrics.LineHeight)
                {
                    float diff = word.MaxVMetrics.Ascender - caret.Y;
                    caret.Y += diff;
                    if (!firstWord)
                    {
                        Span<TextRunGlyph> glyphs = CollectionsMarshal
                            .AsSpan(_context.GlyphBuffer)[new Range(start, end)];
                        foreach (ref TextRunGlyph glyph in glyphs)
                        {
                            glyph.Position.Y += diff;
                        }
                    }
                    maxVMetrics = word.MaxVMetrics;
                }

                start = Math.Min(start, word.GlyphRange.Start.Value);
                end = Math.Max(end, word.GlyphRange.End.Value);
                ascender = MathF.Max(ascender, word.ActualAscender);
                descender = MathF.Max(descender, word.ActualDescender);
                if (firstWord)
                {
                    bbLeft = word.BbLeft;
                }

                Span<TextRunGlyph> span = CollectionsMarshal
                    .AsSpan(_context.GlyphBuffer)[word.GlyphRange];
                foreach (ref TextRunGlyph g in span)
                {
                    g.Position += caret;
                }
                bbRight = caret.X + word.BbRight;
                caret.X += word.AdvanceWidth;
                if (word.HardBreak)
                {
                    break;
                }
            }

            if (notEmpty)
            {
                _firstLine = false;
                Current = new Line
                {
                    GlyphSpan = new Range(start, end),
                    VerticalMetrics = maxVMetrics,
                    BaselineY = caret.Y,
                    BbLeft = bbLeft,
                    BbRight = bbRight,
                    Right = caret.X,
                    ActualAscender = ascender,
                    ActualDescender = descender,
                };
                return true;
            }

            return false;
        }
    }

    internal ref struct WordEnumerator
    {
        private readonly TextLayoutContext _context;
        private CharacterEnumerator _characters;
        private Character? _peekedChar;

        public WordEnumerator(in TextLayoutContext context, ReadOnlySpan<TextRun> textRuns)
        {
            _context = context;
            _characters = new CharacterEnumerator(context, textRuns);
            _peekedChar = null;
            Current = default;
        }

        public Word Current { get; private set; }

        public bool MoveNext()
        {
            float bbLeft = float.NaN, bbRight = 0;
            float caret = 0;
            float caretNoTrail = 0;
            bool hardBreak = false;
            List<TextRunGlyph> glyphBuffer = _context.GlyphBuffer;
            int start = glyphBuffer.Count;
            float ascender = 0, descender = 0;
            VerticalMetrics maxVMetrics = new();
            bool notEmpty = false;
            while (_peekedChar is { } || _characters.MoveNext())
            {
                notEmpty = true;
                Character c = _peekedChar ?? _characters.Current;
                _peekedChar = null;
                if (c.Kind == CharacterKind.RubyText) { break; }
                maxVMetrics = maxVMetrics.Max(c.Font.GetVerticalMetrics(c.FontSize));
                if (!Rune.IsControl(c.Scalar))
                {
                    GlyphDimensions dims = c.Font.GetGlyphDimensions(c.GlyphIndex, c.FontSize);
                    var glyph = new TextRunGlyph
                    {
                        Kind = c.Kind,
                        TextRunIndex = c.TextRun,
                        GlyphIndex = c.GlyphIndex,
                        Position = new Vector2(caret + dims.Left, -dims.Top),
                        IsWhitespace = Rune.IsWhiteSpace(c.Scalar)
                    };
                    glyphBuffer.Add(glyph);
                    caret += dims.Advance;
                    ascender = MathF.Max(ascender, dims.Top);
                    descender = MathF.Max(descender, dims.Height - dims.Top - 1);
                    bbRight = glyph.Position.X + dims.Width;
                    if (float.IsNaN(bbLeft))
                    {
                        bbLeft = dims.Left;
                    }
                    if (!Rune.IsWhiteSpace(c.Scalar) || c.Kind == CharacterKind.RubyBase)
                    {
                        caretNoTrail = caret;
                    }
                }
                if (c.LineBreak is { } lb)
                {
                    hardBreak = lb.Kind == LineBreakKind.Hard;
                    break;
                }
            }

            int end = glyphBuffer.Count;
            if (notEmpty)
            {
                Current = new Word
                {
                    GlyphRange = new Range(start, end),
                    AdvanceWidth = caret,
                    AdvanceWidthNoTrail = caretNoTrail,
                    HardBreak = hardBreak,
                    MaxVMetrics = maxVMetrics,
                    ActualAscender = ascender,
                    ActualDescender = descender,
                    BbLeft = bbLeft,
                    BbRight = bbRight
                };

                if (_characters.Current.Kind == CharacterKind.RubyText)
                {
                    ProcessRubyText();
                }

                return true;
            }

            return false;
        }

        private void ProcessRubyText()
        {
            var chars = new List<Character>();
            float rtWidth = 0, rtWidthNoTrail = 0;
            do
            {
                Character c = _characters.Current;
                if (c.Kind != CharacterKind.RubyText) { break; }
                GlyphDimensions dims = c.Font.GetGlyphDimensions(c.GlyphIndex, c.FontSize);
                rtWidth += dims.Advance;
                if (!Rune.IsWhiteSpace(c.Scalar))
                {
                    rtWidthNoTrail = rtWidth;
                }
                chars.Add(c);
            } while (_characters.MoveNext());

            if (_characters.Current.Kind != CharacterKind.RubyText)
            {
                _peekedChar = _characters.Current;
            }

            Word rb = Current;
            List<TextRunGlyph> glyphBuffer = _context.GlyphBuffer;
            Vector2 rubyCaret = Vector2.Zero;
            if (rtWidthNoTrail <= rb.AdvanceWidthNoTrail)
            {
                // Space the ruby glyphs evenly if the base is long enough
                float blockWidth = MathF.Floor(rb.AdvanceWidthNoTrail / chars.Count);
                float halfBlockWidth = MathF.Floor(blockWidth / 2.0f);
                foreach (Character c in chars)
                {
                    GlyphDimensions dims = c.Font.GetGlyphDimensions(c.GlyphIndex, c.FontSize);
                    var position = new Vector2(
                        rubyCaret.X + halfBlockWidth - MathF.Round(dims.Width / 2.0f),
                        -dims.Top
                    );
                    var glyph = new TextRunGlyph
                    {
                        TextRunIndex = c.TextRun,
                        GlyphIndex = c.GlyphIndex,
                        Position = position,
                        Kind = CharacterKind.RubyText,
                        IsWhitespace = Rune.IsWhiteSpace(c.Scalar)
                    };
                    glyphBuffer.Add(glyph);
                    rubyCaret.X += blockWidth;
                }
            }
            else
            {
                // Otherwise, center the ruby text over the base
                rubyCaret.X -= MathF.Round((rtWidthNoTrail - rb.AdvanceWidthNoTrail) / 2.0f);
                foreach (Character c in chars)
                {
                    GlyphDimensions dims = c.Font.GetGlyphDimensions(c.GlyphIndex, c.FontSize);
                    var position = new Vector2(
                        rubyCaret.X + dims.Left,
                        -dims.Top
                    );
                    var glyph = new TextRunGlyph
                    {
                        TextRunIndex = c.TextRun,
                        GlyphIndex = c.GlyphIndex,
                        Position = position,
                        Kind = CharacterKind.RubyText,
                        IsWhitespace = Rune.IsWhiteSpace(c.Scalar)
                    };
                    glyphBuffer.Add(glyph);
                    rubyCaret.X += dims.Advance;
                }
            }

            var updatedRange = new Range(
                Current.GlyphRange.Start,
                Current.GlyphRange.End.Value + chars.Count
            );
            Current = Current.WithRange(updatedRange);
        }
    }

    internal ref struct CharacterEnumerator
    {
        private ref struct PartInfo
        {
            public FontData Font;
            public SpanRuneEnumerator Scalars;
            public LineBreakEnumerator LineBreaks;
            public LineBreak? NextBreak;
            public int Position;
            public bool MayBreak;

            public static PartInfo None => new()
            {
                Font = null!,
                Scalars = default,
                LineBreaks = default,
                NextBreak = default
            };

            public bool IsNone => Font is null!;
        }

        private readonly TextLayoutContext _context;
        private readonly ReadOnlySpan<TextRun> _textRuns;
        private int _textRun;
        private PartInfo _currentPart;
        private bool _processingRubyText;

        public CharacterEnumerator(in TextLayoutContext context, ReadOnlySpan<TextRun> textRuns)
        {
            _context = context;
            _textRuns = textRuns;
            _currentPart = PartInfo.None;
            _textRun = 0;
            _processingRubyText = false;
            Current = default;
        }

        public Character Current { get; private set; }

        public bool MoveNext()
        {
            while (_textRun < _textRuns.Length)
            {
                ref readonly TextRun textRun = ref _textRuns[_textRun];
                if (_currentPart.IsNone)
                {
                    ReadOnlySpan<char> text = !_processingRubyText
                        ? textRun.Text.Span
                        : textRun.RubyText.Span;
                    _currentPart = new PartInfo
                    {
                        Font = _context.GlyphRasterizer.GetFontData(textRun.Font),
                        Scalars = text.EnumerateRunes(),
                        LineBreaks = new LineBreaker(text).GetEnumerator(),
                        NextBreak = null,
                        MayBreak = !textRun.HasRubyText
                    };
                }

                ref PartInfo part = ref _currentPart;
                if (part.Scalars.MoveNext())
                {
                    Rune scalar = part.Scalars.Current;
                    if ((part.NextBreak is null || part.NextBreak.Value.PosInScalars < part.Position)
                        && part.MayBreak)
                    {
                        part.NextBreak = null;
                        while (part.LineBreaks.MoveNext())
                        {
                            LineBreak lb = part.LineBreaks.Current;
                            if (lb.PosInScalars > part.Position)
                            {
                                part.NextBreak = lb;
                                break;
                            }
                        }
                    }

                    LineBreak? lineBreak = null;
                    if (part.NextBreak is { } nextBreak
                        && nextBreak.PosInScalars == part.Position + 1)
                    {
                        lineBreak = nextBreak;
                    }

                    PtFontSize fontSize = !_processingRubyText
                        ? textRun.FontSize
                        : new PtFontSize((int)(textRun.FontSize.Value * _context.RubyFontSizeMultiplier));
                    CharacterKind kind = CharacterKind.Regular;
                    if (textRun.HasRubyText)
                    {
                        kind = !_processingRubyText
                            ? CharacterKind.RubyBase
                            : CharacterKind.RubyText;
                    }
                    Current = new Character
                    {
                        Kind = kind,
                        Scalar = scalar,
                        TextRun = (uint)_textRun,
                        Font = part.Font,
                        GlyphIndex = part.Font.GetGlyphIndex(scalar),
                        FontSize = fontSize,
                        LineBreak = lineBreak
                    };

                    part.Position++;
                    return true;
                }

                _currentPart = PartInfo.None;
                if (!textRun.HasRubyText || _processingRubyText)
                {
                    _textRun++;
                    _processingRubyText = false;
                }
                else
                {
                    _processingRubyText = true;
                }
            }

            return false;
        }
    }
}
