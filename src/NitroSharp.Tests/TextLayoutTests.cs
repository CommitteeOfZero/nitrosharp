using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NitroSharp.Text;
using Veldrid;
using Xunit;

using PhysicalRectF = NitroSharp.RectangleF<NitroSharp.ScreenPixel>;
using DesignDimension = NitroSharp.DimensionU<NitroSharp.DesignPixel>;

namespace NitroSharp.Tests
{
    public sealed class FontContext : IDisposable
    {
        public FontContext()
        {
            Rasterizer = new GlyphRasterizer();
            Rasterizer.AddFonts(Directory.EnumerateFiles("Fonts"));
            DefaultFont = new FontFaceKey("VL Gothic", FontStyle.Regular);
        }

        internal GlyphRasterizer Rasterizer { get; }
        internal FontFaceKey DefaultFont { get; }

        public void Dispose()
        {
            Rasterizer.Dispose();
        }
    }

    public class TextLayoutTests : IClassFixture<FontContext>
    {
        private readonly GlyphRasterizer _rasterizer;
        private readonly FontFaceKey _font;

        public TextLayoutTests(FontContext context)
        {
            _rasterizer = context.Rasterizer;
            _font = context.DefaultFont;
        }

        [Fact]
        public void Chat()
        {
            var layout = new TextLayout(maxWidth: 400, maxHeight: 25);
            layout.Append(_rasterizer, Regular("ナイトハルト：\"パンモロ"));
            layout.Append(_rasterizer, Regular("\"より\"はいてない"));
            layout.Append(_rasterizer, Regular("\"の方がいいだろ？　それと同じことさ"));
            Assert.Equal(2, layout.Lines.Length);
        }

        [Fact]
        public void HardBreaks()
        {
            const string s = @"Lorem ipsum dolor sit amet,
consectetur adipiscing elit.
Nullam nisl ipsum, semper ac lacus sit amet, semper viverra diam.";

            TextLayout layout = Layout(s, null, null);
            AssertLines(layout, s.Split('\n'));
        }

        [Fact]
        public void HardBreak_InSeparateTextRun()
        {
            TextRun[] runs =
            {
                Regular("meow"),
                Regular("\n"),
                Regular("meow")
            };

            TextLayout layout = Layout(runs, null, null);
            AssertLines(layout, new[] { "meow", "meow" });
        }

        [Fact]
        public void ConstrainedHeight()
        {
            const string text = "meow meow meow meow meow meow ";
            TextLayout layout = Layout(text, maxW: 128, maxH: 50);
            AssertLines(layout, Enumerable.Repeat("meow meow ", 2).ToArray());
            IEnumerable<int> lengths = layout.Lines.ToArray()
                .Select(x => layout.GetGlyphSpanLength(x.GlyphSpan));
            Assert.Equal(new[] { 10, 10 }, lengths);
        }

        [Fact]
        public void ConstainedWidth()
        {
            // w: 40
            const string text = "meow meow";
            TextLayout layout = Layout(text, maxW: 41, null);
            AssertLines(layout, new[] { "meow", "meow" });
        }

        [Fact]
        public void ConstrainedWidth_AppendSeparately()
        {
            TextLayout layout = new(maxWidth: 41);
            layout.Append(_rasterizer, Regular("meow"));
            layout.Append(_rasterizer, Regular("meow"));
            AssertLines(layout, new[] { "meow", "meow" });
        }

        [Fact]
        public void SoftBreaks_English()
        {
            const string text = "meow meow meow meow meow meow ";
            TextLayout layout = Layout(text, maxW: 128, null);
            AssertLines(layout, Enumerable.Repeat("meow meow ", 3).ToArray());
            IEnumerable<int> lengths = layout.Lines.ToArray()
                .Select(x => layout.GetGlyphSpanLength(x.GlyphSpan));
            Assert.Equal(new[] { 10, 10, 10 }, lengths);
        }

        [Fact]
        public void MultipleRuns_NoBreaks()
        {
            TextRun[] runs =
            {
                Regular("A gaze "),
                Regular("falls "),
                Regular("from the sky.")
            };

            TextLayout layout = Layout(runs, null, null);
            AssertText(layout, "A gaze falls from the sky.");
        }

        [Fact]
        public void MultipleRuns_AppendEach_NoBreaks()
        {
            TextRun[] runs =
            {
                Regular("A gaze "),
                Regular("falls "),
                Regular("from the sky.")
            };

            TextLayout layout = new();
            foreach (TextRun run in runs)
            {
                layout.Append(_rasterizer, run);
            }

            AssertText(layout, "A gaze falls from the sky.");
        }

        [Fact]
        public void LastRun_SingleCharacter()
        {
            TextRun[] runs =
            {
                Regular("\""),
                Regular("meow"),
                Regular("\"")
            };

            TextLayout layout = new();
            layout.Append(_rasterizer, runs);
            AssertText(layout, "\"meow\"");
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        public void StartWithNewline(string newline)
        {
            TextLayout withoutLinebreak = Layout("meow", null, null);

            TextRun[] runs =
            {
                Regular(newline),
                Regular("meow")
            };

            TextLayout layout = Layout(runs, null, null);
            Assert.True(layout.Lines[0].IsEmpty);
            AssertLine(layout, layout.Lines[1], "meow");
            for (int i = 0; i < "meow".Length; i++)
            {
                Assert.True(layout.Glyphs[i].Position.Y > withoutLinebreak.Glyphs[i].Position.Y);
            }
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        public void MultipleLines_AppendEach(string newline)
        {
            TextLayout layout = new();
            layout.Append(_rasterizer, Regular("A gaze"));
            layout.Append(_rasterizer, Regular(newline));
            layout.Append(_rasterizer, Regular("falls from the sky."));
            AssertLines(layout, new[] { "A gaze", "falls from the sky." });
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void AppendText_StartingWithNewline(string newline)
        {
            TextLayout layout = new();
            layout.Append(_rasterizer, Regular("A gaze"));
            layout.Append(_rasterizer, Regular($"{newline}falls from the sky."));
            AssertLines(layout, new[] { "A gaze", "falls from the sky." });
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void AppendNewline(string newline)
        {
            TextLayout a = new(maxWidth: new[] { Regular("a") });
            float prevY = a.Glyphs[0].Position.Y;
            TextLayout prev = new();
            for (int nbLines = 1; nbLines <= 4; nbLines++)
            {
                TextLayout layout = new();
                for (int i = 0; i < nbLines; i++)
                {
                    layout.Append(_rasterizer, Regular(newline));
                }

                Assert.Equal(nbLines, layout.Lines.Length);
                layout.Append(_rasterizer, Regular("a"));
                Assert.Equal(nbLines + 1, layout.Lines.Length);
                float y = layout.Glyphs[^1].Position.Y;
                Assert.True(y > prevY);
                prevY = y;
                prev = layout;
            }
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void AppendText_EndingWithNewline(string newline)
        {
            TextLayout layout = new();
            layout.Append(_rasterizer, Regular($"A gaze{newline}"));
            layout.Append(_rasterizer, Regular("fell from the sky."));
            AssertLines(layout, new[] { "A gaze", "fell from the sky." });
        }

        [Fact]
        public void AppendText_SameLine()
        {
            TextLayout layout = Layout("A gaze", null, null);
            PhysicalRectF bbBefore = layout.BoundingBox;
            TextRun second = Regular(" falls from the sky.");
            layout.Append(_rasterizer, second);
            PhysicalRectF bbAfter = layout.BoundingBox;
            AssertText(layout, "A gaze falls from the sky.");
            Assert.True(bbAfter.Width > bbBefore.Width);

            Assert.Equal(
                layout.GlyphRuns[0].GlyphSpan.End.Value,
                layout.GlyphRuns[1].GlyphSpan.Start.Value
            );
        }

        [Theory]
        [InlineData("ruby base", "ruby text")]
        [InlineData("ru", "ruby text")]
        public void RubyText(string rb, string rt)
        {
            TextRun run = Ruby(rb, rt);
            TextLayout layout = Layout(new[] { run }, null, null, 45);

            GlyphRun rbRun = layout.GlyphRuns[0];
            GlyphRun rtRun = layout.GlyphRuns[1];

            Assert.Equal(GlyphRunFlags.RubyBase, rbRun.Flags);
            Assert.Equal(GlyphRunFlags.RubyText, rtRun.Flags);
            Assert.True(rtRun.FontSize.Value.ToSingle() < rbRun.FontSize.Value.ToSingle());

            AssertGlyphs(layout, rbRun.GlyphSpan, rb);
            AssertGlyphs(layout, rtRun.GlyphSpan, rt);

            PositionedGlyph[] rubyBase = layout.Glyphs[rbRun.GlyphSpan].ToArray();
            PositionedGlyph[] rubyText = layout.Glyphs[rtRun.GlyphSpan].ToArray();
            float rbMinY = rubyBase.Min(x => x.Position.Y);
            float rtMaxY = rubyText.Max(x => x.Position.Y);
            Assert.True(rbMinY > rtMaxY);
        }

        private void AssertLines(TextLayout layout, string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                AssertLine(layout, layout.Lines[i], lines[i]);
                if (i > 0)
                {
                    PositionedGlyph curStart = layout.Glyphs[layout.Lines[i].GlyphSpan.Start.Value];
                    PositionedGlyph prevEnd = layout.Glyphs[layout.Lines[i - 1].GlyphSpan.End.Value - 1];
                    Assert.True(curStart.Position.Y > prevEnd.Position.Y);
                }
            }
        }

        private void AssertLine(TextLayout layout, Line line, string text)
        {
            AssertGlyphs(layout, line.GlyphSpan, text);
            PositionedGlyph[] glyphs = layout.Glyphs[line.GlyphSpan].ToArray();
            var positions = glyphs.Select(x => x.Position.X)
                .ToArray();
            var orderedByX = positions.OrderBy(x => x);
            Assert.Equal(orderedByX, positions);
        }

        private void AssertText(TextLayout layout, string text)
        {
            Assert.True(layout.Lines.Length == 1);
            AssertLine(layout, layout.Lines[0], text);
            var span = new Range(layout.GlyphRuns[0].GlyphSpan.Start, layout.GlyphRuns[^1].GlyphSpan.End);
            AssertGlyphs(layout, span, text);
        }

        private void AssertGlyphs(TextLayout layout, Range span, string text)
        {
            ReadOnlySpan<PositionedGlyph> glyphs = layout.Glyphs[span];
            FontData fontData  = _rasterizer.GetFontData(_font);
            int pos = 0;
            foreach (Rune scalar in text.EnumerateRunes())
            {
                if (!Rune.IsControl(scalar))
                {
                    uint expected = fontData.GetGlyphIndex(scalar);
                    Assert.Equal(expected, glyphs[pos].Index);
                    pos++;
                }
            }
        }

        private TextLayout Layout(TextRun[] textRuns, DesignDimension? maxW, DesignDimension? maxH, DesignDimension? fixedLineHeight = null)
        {
            return new(_rasterizer, textRuns, maxWidth: maxH, maxHeight: fixedLineHeight);
        }

        private TextLayout Layout(string s, uint? maxW, uint? maxH)
        {
            TextRun run = Regular(s);
            return new TextLayout(_rasterizer, new[] { run }, maxW, maxH);
        }

        private TextRun Regular(string s)
        {
            return TextRun.Regular(
                s.AsMemory(),
                _font,
                new PtFontSize(20),
                RgbaFloat.Black,
                null
            );
        }

        private TextRun Ruby(string rubyBase, string rubyText)
        {
            return TextRun.WithRubyText(
                rubyBase.AsMemory(),
                rubyText.AsMemory(),
                _font,
                new PtFontSize(20),
                RgbaFloat.Black,
                null
            );
        }
    }
}
