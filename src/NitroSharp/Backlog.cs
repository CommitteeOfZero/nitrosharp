using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal readonly struct BacklogEntry
    {
        public readonly GlyphSpan GlyphSpan;

        public BacklogEntry(GlyphSpan glyphSpan)
        {
            GlyphSpan = glyphSpan;
        }
    }

    internal sealed class Backlog
    {
        private readonly FontConfiguration _fontConfig;
        private readonly TextLayout _layout = new TextLayout(1042, null, fixedLineHeight: 43);

        public Backlog(FontConfiguration fontConfig)
        {
            _fontConfig = fontConfig;
        }

        public TextLayout TextLayout => _layout;

        public GlyphRun GetGlyphRun(GlyphSpan span)
        {
            return new GlyphRun(
                _fontConfig.DefaultFont,
                new PtFontSize(36),
                RgbaFloat.Black,
                RgbaFloat.White,
                span,
                GlyphRunFlags.Outline
            );
        }

        public BacklogEntry Append(GlyphRasterizer glyphRasterizer, TextSegment text)
        {
            _layout.NewLine(glyphRasterizer);

            uint start = (uint)_layout.Glyphs.Length;
            foreach (TextRun textRun in text.TextRuns)
            {
                if (!textRun.HasRubyText)
                {
                    var run = TextRun.Regular(
                        textRun.Text,
                        _fontConfig.DefaultFont,
                        new PtFontSize(36),
                        RgbaFloat.Black,
                        RgbaFloat.White
                    );
                    _layout.Append(glyphRasterizer, run);
                }
            }

            var span = new GlyphSpan(start, (uint)(_layout.Glyphs.Length - start));
            return new BacklogEntry(span);
        }

        public void Clear()
        {
            _layout.Clear();
        }
    }
}
