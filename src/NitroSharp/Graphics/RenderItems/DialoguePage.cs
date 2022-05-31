using System.Collections.Generic;
using System.Numerics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;
using NitroSharp.Text;

namespace NitroSharp.Graphics
{
    internal sealed class DialoguePage : RenderItem2D
    {
        private enum ConsumeResult
        {
            KeepGoing,
            Halt,
            AllDone
        }

        private readonly DesignSizeU? _bounds;
        private readonly DesignMarginU _margin;
        private readonly DesignDimensionU _lineHeight;
        private readonly NsScriptThread _dialogueThread;
        private readonly TextLayout _layout;
        private readonly List<string> _lines = new();
        private readonly Queue<DialogueSegment> _remainingSegments = new();

        private TypewriterAnimation? _animation;
        private bool _skipping;

        public DialoguePage(
            in ResolvedEntityPath path,
            int priority,
            DesignSizeU? bounds,
            DesignDimensionU lineHeight,
            in DesignMarginU margin,
            NsScriptThread dialogueThread,
            RenderContext renderContext)
            : base(path, priority)
        {
            _margin = margin;
            _bounds = bounds;
            _lineHeight = lineHeight;
            _dialogueThread = dialogueThread;
            _layout = new TextLayout(
                renderContext.WorldToDeviceScale,
                maxWidth: bounds?.TypedWidth,
                maxHeight: null,
                fixedLineHeight: lineHeight
            );
        }

        public override EntityKind Kind => EntityKind.DialoguePage;

        protected bool ScaleAutomatically => false;

        public override bool IsIdle => _dialogueThread.DoneExecuting && LineRead;
        public bool LineRead { get; private set; }
        public bool DisableAnimation { get; set; }

        public DialoguePage(
            in ResolvedEntityPath path,
            in DialoguePageSaveData saveData,
            GameLoadingContext loadCtx)
            : base(path, saveData.Common)
        {
            _bounds = saveData.Bounds;
            _lineHeight = saveData.LineHeight;
            _layout = new TextLayout(
                loadCtx.Rendering.WorldToDeviceScale,
                maxWidth: _bounds?.TypedWidth,
                maxHeight: null,
                fixedLineHeight: _lineHeight
            );
            _dialogueThread = loadCtx.Process.VmProcess.GetThread(saveData.DialogueThreadId);
            _margin = saveData.Margin;

            foreach (string line in saveData.Lines)
            {
                _lines.Add(line);
                FontSettings fontConfig = loadCtx.Process.FontSettings;
                var buffer = Dialogue.Parse(line, fontConfig);
                foreach (DialogueSegment seg in buffer.Segments)
                {
                    _remainingSegments.Enqueue(seg);
                }
            }

            while (_remainingSegments.Count != saveData.SegmentsRemaining)
            {
                ConsumeSegment(loadCtx.GameContext);
            }

            loadCtx.Rendering.Text.RequestGlyphs(_layout);
        }

        public void Append(GameContext ctx, string markup, FontSettings fontSettings)
        {
            _lines.Add(markup);
            foreach (DialogueSegment seg in Dialogue.Parse(markup, fontSettings).Segments)
            {
                _remainingSegments.Enqueue(seg);
            }

            Advance(ctx);
            ctx.RenderContext.Text.RequestGlyphs(_layout);
            LineRead = false;
        }

        public override DesignSize GetUnconstrainedBounds(RenderContext ctx)
        {
            DesignRect bb = _layout.BoundingBox.Convert(ctx.DeviceToWorldScale);
            var size = new DesignSize(
                _margin.Left + bb.Right + _margin.Right,
                _margin.Top + bb.Bottom + _margin.Bottom
            );
            return size.Constrain(_layout.GetMaxDesignBounds(ctx).ToSizeF());
        }

        public void Clear()
        {
            _layout.Clear();
            _remainingSegments.Clear();
            _lines.Clear();
            _animation = null;
        }

        private bool Advance(GameContext ctx)
        {
            if (ctx.Advance && _animation is not null)
            {
                if (!_animation.Skipping)
                {
                    _animation.Skip();
                }

                return true;
            }

            if (_remainingSegments.Count == 0)
            {
                return false;
            }

            while (ConsumeSegment(ctx) == ConsumeResult.KeepGoing)
            {
            }

            return true;
        }

        protected override void Update(GameContext ctx)
        {
            _skipping = ctx.Skipping;
            bool advance = ctx.Advance || ctx.Skipping;
            if (advance || _dialogueThread.DoneExecuting)
            {
                LineRead = _remainingSegments.Count == 0 && _animation is null;
            }
            if (advance && Advance(ctx))
            {
                ctx.Advance = false;
            }

            ctx.RenderContext.Text.RequestGlyphs(_layout);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            PhysicalRect rect = DeviceBoundingRect;
            PhysicalPoint offset = new DesignPoint(_margin.Left, _margin.Top).Convert(ctx.WorldToDeviceScale);
            ctx.Text.Render(ctx, batch, _layout, WorldMatrix, offset, rect, Color.A);

            if (_animation is null && !_skipping)
            {
                float x = ctx.SystemVariables.PositionXTextIcon.AsNumber()!.Value;
                float y = ctx.SystemVariables.PositionYTextIcon.AsNumber()!.Value;
                ctx.Icons.WaitLine.Render(ctx, new Vector2(x, y));
            }
        }

        private bool AnimationEnabled(GameContext ctx) => !ctx.Skipping && !DisableAnimation;

        private ConsumeResult ConsumeSegment(GameContext ctx)
        {
            GlyphRasterizer glyphRasterizer = ctx.RenderContext.GlyphRasterizer;
            if (_remainingSegments.TryDequeue(out DialogueSegment? seg))
            {
                switch (seg.SegmentKind)
                {
                    case DialogueSegmentKind.Text:
                        var textSegment = (TextSegment)seg;
                        int start = _layout.GlyphRuns.Length;
                        _layout.Append(glyphRasterizer, textSegment.TextRuns.AsSpan());
                        ctx.Backlog.Append(textSegment);
                        if (AnimationEnabled(ctx))
                        {
                            if (_animation is null)
                            {
                                _animation = new TypewriterAnimation(
                                    _layout,
                                    _layout.GlyphRuns[start..], 40
                                );
                            }
                            else
                            {
                                _animation.Append(_layout, _layout.GlyphRuns[start..]);
                            }
                        }
                        return ConsumeResult.KeepGoing;
                    case DialogueSegmentKind.Marker:
                        var marker = (MarkerSegment)seg;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                return ConsumeResult.Halt;
                        }
                        break;
                    case DialogueSegmentKind.Voice:
                        var voice = (VoiceSegment)seg;
                        if (voice.Action == NsVoiceAction.Play)
                        {
                            ctx.PlayVoice(voice.CharacterName, voice.FileName);
                        }
                        else
                        {
                            ctx.StopVoice();
                        }
                        break;

                }

                return ConsumeResult.KeepGoing;
            }

            return ConsumeResult.AllDone;
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

        public new DialoguePageSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            Bounds = _bounds,
            LineHeight = _lineHeight,
            Margin = _margin,
            DialogueThreadId = _dialogueThread.Id,
            Lines = _lines.ToArray(),
            SegmentsRemaining = _remainingSegments.Count
        };
    }

    [Persistable]
    internal readonly partial struct DialoguePageSaveData : IEntitySaveData
    {
        public RenderItemSaveData Common { get; init; }
        public DesignSizeU? Bounds { get; init; }
        public DesignDimensionU LineHeight { get; init; }
        public DesignMarginU Margin { get; init; }
        public uint DialogueThreadId { get; init; }
        public string[] Lines { get; init; }
        public int SegmentsRemaining { get; init; }

        public EntitySaveData CommonEntityData => Common.EntityData;
    }
}
