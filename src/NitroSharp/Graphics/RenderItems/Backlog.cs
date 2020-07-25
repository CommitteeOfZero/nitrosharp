using NitroSharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Backlog : RenderItem2D
    {
        private readonly List<string> _history;
        private readonly TextLayout _layout;
        private int _lineCount;

        public Backlog(
            in ResolvedEntityPath path,
            int priority,
            List<string> history)
            : base(path, priority)
        {
            _history = history;
            _layout = new TextLayout(fixedLineHeight: 44);
        }

        protected override void Update(GameContext ctx)
        {
            if (_lineCount != _history.Count)
            {
                foreach (string s in _history.Skip(_lineCount))
                {
                    if (_layout.Glyphs.Length > 0)
                    {
                        _layout.StartNewLine(ctx.GlyphRasterizer, (uint)_layout.Glyphs.Length);
                    }

                    _layout.Append(
                        ctx.GlyphRasterizer,
                        new[]
                        {
                            TextRun.Regular(
                                s.AsMemory(),
                                ctx.FontConfig.DefaultFont,
                                new PtFontSize(36),
                                RgbaFloat.Black,
                                null
                            )
                        }
                    );

                    ctx.RenderContext.Text.RequestGlyphs(_layout);
                }
                _lineCount = _history.Count;
            }
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            RectangleF br = BoundingRect;
            var rect = new RectangleU((uint)br.X, (uint)br.Y, (uint)br.Width, (uint)br.Height);
            ctx.Text.Render(ctx, batch, _layout, WorldMatrix, new Vector2(100, 80), new RectangleU(0, 0, 1280, 580));
        }

        public void Clear() => _layout.Clear();

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            return _layout.BoundingBox.Size.ToSize();
        }
    }
}
