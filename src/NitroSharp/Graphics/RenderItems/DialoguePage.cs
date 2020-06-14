using System;
using System.Numerics;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;

namespace NitroSharp.Graphics
{
    internal sealed class DialoguePage : RenderItem2D
    {
        private readonly ThreadContext _dialogueThread;
        private readonly TextLayout _layout;
        private ReadOnlyMemory<TextBufferSegment> _remainingSegments;

        public DialoguePage(
            in ResolvedEntityPath path,
            int priority,
            Size? bounds,
            float lineHeight,
            GlyphRasterizer glyphRasterizer,
            ThreadContext dialogueThread)
            : base(path, priority)
        {
            _dialogueThread = dialogueThread;
            _layout = new TextLayout(glyphRasterizer, Array.Empty<TextRun>(), bounds, lineHeight);
        }

        public override bool IsIdle => _dialogueThread.DoneExecuting;

        public void Load(TextBuffer text)
        {
            _layout.Clear();
            _remainingSegments = text.Segments.AsMemory();
        }

        protected override void Update(GameContext ctx)
        {
            InputContext inputCtx = ctx.InputContext;
            GlyphRasterizer glyphRasterizer = ctx.GlyphRasterizer;
            bool advance = _layout.Glyphs.IsEmpty || inputCtx.VKeyDown(VirtualKey.Advance);
            if (advance)
            {
                while (!_remainingSegments.IsEmpty)
                {
                    TextBufferSegment seg = _remainingSegments.Span[0];
                    _remainingSegments = _remainingSegments[1..];
                    switch (seg.SegmentKind)
                    {
                        case TextBufferSegmentKind.Text:
                            var textSegment = (TextSegment)seg;
                            _layout.Append(glyphRasterizer, textSegment.TextRuns.AsSpan());
                            break;
                        case TextBufferSegmentKind.Marker:
                            var marker = (MarkerSegment)seg;
                            switch (marker.MarkerKind)
                            {
                                case MarkerKind.Halt:
                                    return;
                            }
                            break;
                    }
                }
            }

            ctx.RenderContext.Text.RequestGlyphs(_layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            ctx.Text.Render(ctx, batch, _layout, WorldMatrix, Vector2.Zero);
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            RectangleF bb = _layout.BoundingBox;
            return new Size(
                (uint)(bb.Right),
                (uint)(bb.Bottom)
            );
        }
    }
}
