using System.Collections.Generic;
using System.Numerics;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class DialoguePage : RenderItem2D
    {
        private readonly ThreadContext _dialogueThread;
        private readonly TextLayout _layout;
        private readonly Queue<TextBufferSegment> _remainingSegments;

        private TypewriterAnimation? _animation;

        public DialoguePage(
            in ResolvedEntityPath path,
            int priority,
            Size? bounds,
            float lineHeight,
            in Vector4 margin,
            ThreadContext dialogueThread)
            : base(path, priority)
        {
            Margin = margin;
            _dialogueThread = dialogueThread;
            _layout = new TextLayout(bounds, lineHeight);
            _remainingSegments = new Queue<TextBufferSegment>();
        }

        public Vector4 Margin { get; }
        public override bool IsIdle => _dialogueThread.DoneExecuting && LineRead;
        public bool LineRead { get; private set; }

        public void Append(RenderContext renderCtx, TextBuffer text)
        {
            foreach (TextBufferSegment seg in text.Segments)
            {
                _remainingSegments.Enqueue(seg);
            }

            Advance(renderCtx);
            renderCtx.Text.RequestGlyphs(_layout);
            LineRead = false;
        }

        private void Advance(RenderContext renderCtx)
        {
            if (_animation is object)
            {
                if (!_animation.Skipping)
                {
                    _animation.Skip();
                }

                return;
            }

            int start = _layout.GlyphRuns.Length;
            while (_remainingSegments.TryDequeue(out TextBufferSegment? seg))
            {
                switch (seg.SegmentKind)
                {
                    case TextBufferSegmentKind.Text:
                        var textSegment = (TextSegment)seg;
                        _layout.Append(renderCtx.GlyphRasterizer, textSegment.TextRuns.AsSpan());
                        break;
                    case TextBufferSegmentKind.Marker:
                        var marker = (MarkerSegment)seg;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                goto exit;
                        }
                        break;
                }
            }

        exit:
            if (_layout.GlyphRuns.Length != start)
            {
                _animation = new TypewriterAnimation(_layout, _layout.GlyphRuns[start..], 40);
                renderCtx.Icons.WaitLine.Reset();
            }
        }

        protected override void AdvanceAnimations(RenderContext ctx, float dt, bool assetsReady)
        {
            AdvanceAnimation(ref _animation, dt);
            if (_animation is null)
            {
                ctx.Icons.WaitLine.Update(dt);
            }
            base.AdvanceAnimations(ctx, dt, assetsReady);
        }

        protected override void Update(GameContext ctx)
        {
            bool advance = ctx.InputContext.VKeyDown(VirtualKey.Advance);
            if (advance)
            {
                LineRead = _remainingSegments.Count == 0 && _animation is null;
                Advance(ctx.RenderContext);
            }

            ctx.RenderContext.Text.RequestGlyphs(_layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            RectangleF br = BoundingRect;
            var rect = new RectangleU((uint)br.X, (uint)br.Y, (uint)br.Width, (uint)br.Height);
            ctx.Text.Render(ctx, batch, _layout, WorldMatrix, Margin.XY(), rect);

            if (_animation is null)
            {
                float x = ctx.SystemVariables.PositionXTextIcon.AsNumber()!.Value;
                float y = ctx.SystemVariables.PositionYTextIcon.AsNumber()!.Value;
                ctx.Icons.WaitLine.Render(ctx, new Vector2(x, y));
            }

            return;

            RectangleF bb = _layout.BoundingBox;
            ctx.MainBatch.PushQuad(
                QuadGeometry.Create(
                    new SizeF(bb.Size.Width, bb.Size.Height),
                    WorldMatrix * Matrix4x4.CreateTranslation(new Vector3(Margin.XY() + bb.Position, 0)),
                    Vector2.Zero,
                    Vector2.One,
                    new Vector4(0, 0.8f, 0.0f, 0.3f)
                ).Item1,
                ctx.WhiteTexture,
                ctx.WhiteTexture,
                default,
                BlendMode,
                FilterMode
            );

            var rasterizer = ctx.Text.GlyphRasterizer;
            foreach (var glyphRun in _layout.GlyphRuns)
            {
                var glyphs = _layout.GetGlyphs(glyphRun.GlyphSpan);
                var font = rasterizer.GetFontData(glyphRun.Font);
                foreach (PositionedGlyph g in glyphs)
                {
                    var dims = font.GetGlyphDimensions(g.Index, glyphRun.FontSize);

                    ctx.MainBatch.PushQuad(
                        QuadGeometry.Create(
                            new SizeF(dims.Width, dims.Height),
                            WorldMatrix * Matrix4x4.CreateTranslation(new Vector3(Margin.XY() + g.Position + new Vector2(0, 0), 0)),
                            Vector2.Zero,
                            Vector2.One,
                            new Vector4(0.8f, 0.0f, 0.0f, 0.3f)
                        ).Item1,
                        ctx.WhiteTexture,
                        ctx.WhiteTexture,
                        default,
                        BlendMode,
                        FilterMode
                    );
                }
            }
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            RectangleF bb = _layout.BoundingBox;
            var size = new Size(
                (uint)(Margin.X + bb.Right + Margin.Z),
                (uint)(Margin.Y + bb.Bottom + Margin.W)
            );
            return size.Constrain(_layout.MaxBounds);
        }

        public void Clear()
        {
            _layout.Clear();
            _remainingSegments.Clear();
        }
    }
}
