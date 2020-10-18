using System;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Text;

namespace NitroSharp.Graphics
{
    internal sealed class BacklogView : RenderItem2D
    {
        private const int MaxLines = 12;

        private readonly Backlog _backlog;
        private (int start, int end) _range;

        public BacklogView(
            in ResolvedEntityPath path,
            int priority,
            Backlog backlog)
            : base(path, priority)
        {
            _backlog = backlog;
        }

        public EntityId Scrollbar { get; internal set; }

        public void Scroll(float position)
        {
            position = 1.0f - position;
            TextLayout layout = _backlog.TextLayout;
            int totalLines = layout.Lines.Length;
            int first = (int)Math.Round(position * Math.Max(0, totalLines - MaxLines));
            int last = Math.Min(first + MaxLines - 1, totalLines - 1);
            _range = (first, last + 1);
        }

        protected override void Update(GameContext ctx)
        {
            if (ctx.ActiveProcess.World.Get(Scrollbar) is Scrollbar scrollbar)
            {
                Scroll(scrollbar.GetValue());
            }
            else
            {
                Scroll(1.0f);
            }
            ctx.RenderContext.Text.RequestGlyphs(_backlog.TextLayout);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            TextLayout layout = _backlog.TextLayout;
            Line firstLine = layout.Lines[_range.start];
            Line lastLine = layout.Lines[_range.end - 1];
            uint start = firstLine.GlyphSpan.Start;
            uint end = lastLine.GlyphSpan.End;
            var span = new GlyphSpan(start, end - start);
            GlyphRun run = _backlog.GetGlyphRun(span);
            ReadOnlySpan<GlyphRun> runs = MemoryMarshal.CreateReadOnlySpan(ref run, 1);

            float x = ctx.SystemVariables.BacklogPositionX.AsNumber()!.Value;
            float y = ctx.SystemVariables.BacklogPositionY.AsNumber()!.Value;
            float rowInterval = ctx.SystemVariables.BacklogRowInterval.AsNumber()!.Value;

            var offset = new Vector2(x, 100 - firstLine.BaselineY);
            ctx.Text.Render(
                ctx,
                batch,
                _backlog.TextLayout,
                runs,
                WorldMatrix,
                offset,
                new RectangleU((uint)x, (uint)y, layout.MaxBounds.Width, 584),
                Color.A
            );
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            return _backlog.TextLayout.BoundingBox.Size.ToSize();
        }
    }
}
