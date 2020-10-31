using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class BacklogView : RenderItem2D
    {
        private const int MaxLines = 12;

        private readonly Backlog _backlog;
        private readonly FontConfiguration _fontConfig;
        private readonly TextLayout _textLayout;
        private (int start, int end) _range;
        private GlyphRun _glyphRun;

        private int _entriesAdded;

        public BacklogView(
            in ResolvedEntityPath path,
            int priority,
            GameContext ctx)
            : base(path, priority)
        {
            _backlog = ctx.Backlog;
            _fontConfig = ctx.ActiveProcess.FontConfig;
            float lineHeight = ctx.VM.SystemVariables.BacklogRowInterval.AsNumber()!.Value;
            _textLayout = new TextLayout(1042, null, lineHeight);
        }

        public EntityId Scrollbar { get; internal set; }

        public void Scroll(float position)
        {
            position = 1.0f - position;
            TextLayout layout = _textLayout;
            int totalLines = layout.Lines.Length;
            int first = (int)Math.Round(position * Math.Max(0, totalLines - MaxLines));
            int last = Math.Min(first + MaxLines - 1, Math.Max(totalLines - 1, 0));
            _range = (first, last + 1);
        }

        protected override void Update(GameContext ctx)
        {
            if (_entriesAdded < _backlog.Entries.Length)
            {
                foreach (BacklogEntry entry in _backlog.Entries[_entriesAdded..])
                {
                    _textLayout.NewLine(ctx.GlyphRasterizer);

                    var run = TextRun.Regular(
                        entry.Text.AsMemory(),
                        _fontConfig.DefaultFont,
                        new PtFontSize(36),
                        RgbaFloat.Black,
                        RgbaFloat.White
                    );
                    _textLayout.Append(ctx.GlyphRasterizer, run);
                    _entriesAdded++;
                }
            }

            if (ctx.ActiveProcess.World.Get(Scrollbar) is Scrollbar scrollbar)
            {
                Scroll(scrollbar.GetValue());
            }
            else
            {
                Scroll(1.0f);
            }


            Line firstLine = _textLayout.Lines[_range.start];
            Line lastLine = _textLayout.Lines[_range.end - 1];
            uint start = firstLine.GlyphSpan.Start;
            uint end = lastLine.GlyphSpan.End;
            var span = new GlyphSpan(start, end - start);
            _glyphRun = GetGlyphRun(span);

            ctx.RenderContext.Text.RequestGlyphs(_textLayout, _glyphRun);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            Line firstLine = _textLayout.Lines[_range.start];

            float x = ctx.SystemVariables.BacklogPositionX.AsNumber()!.Value;
            float y = ctx.SystemVariables.BacklogPositionY.AsNumber()!.Value;
            var offset = new Vector2(x, 100 - firstLine.BaselineY);
            ctx.Text.Render(
                ctx,
                batch,
                _textLayout,
                _glyphRun,
                WorldMatrix,
                offset,
                new RectangleU((uint)x, (uint)y, _textLayout.MaxBounds.Width, 584),
                Color.A
            );
        }

        private GlyphRun GetGlyphRun(GlyphSpan span)
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

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            return _textLayout.BoundingBox.Size.ToSize();
        }
    }
}
