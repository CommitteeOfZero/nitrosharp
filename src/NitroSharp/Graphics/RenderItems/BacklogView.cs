using System;
using System.Numerics;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class BacklogView : RenderItem2D
    {
        private readonly Backlog _backlog;
        private readonly FontSettings _fontSettings;
        private readonly TextLayout _textLayout;
        private readonly uint _lineHeight;
        private readonly uint _visibleHeight;
        private readonly int _maxLines;

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
            _fontSettings = ctx.ActiveProcess.FontSettings;
            SystemVariableLookup sys = ctx.VM.SystemVariables;

            uint x = (uint)sys.BacklogPositionX.AsNumber()!.Value;
            uint y = (uint)sys.BacklogPositionY.AsNumber()!.Value;
            Transform.Position = new Vector3(x, y, 0);

            uint glyphWidth = (uint)sys.BacklogCharacterWidth.AsNumber()!.Value;
            const uint glyphsPerLine = 32; // called "word_in_row" in system.ini
            uint maxWidth = glyphWidth * glyphsPerLine;
            _lineHeight = (uint)sys.BacklogRowInterval.AsNumber()!.Value;
            _maxLines = (int)sys.BacklogRowMax.AsNumber()!.Value;
            _visibleHeight = (uint)(_lineHeight * _maxLines);
            _textLayout = new TextLayout(maxWidth, null, _lineHeight);
        }

        public EntityId Scrollbar { get; internal set; }

        public override EntityKind Kind => EntityKind.BacklogView;

        public void Scroll(float position)
        {
            position = 1.0f - position;
            TextLayout layout = _textLayout;
            int totalLines = layout.Lines.Length;
            int first = (int)Math.Round(position * Math.Max(0, totalLines - _maxLines));
            int last = Math.Min(first + _maxLines - 1, Math.Max(totalLines - 1, 0));
            _range = (first, last + 1);
        }

        protected override void Update(GameContext ctx)
        {
            if (_entriesAdded < _backlog.Entries.Length)
            {
                foreach (BacklogEntry entry in _backlog.Entries[_entriesAdded..])
                {
                    var run = TextRun.Regular(
                        entry.Text.AsMemory(),
                        _fontSettings.DefaultFont,
                        _fontSettings.DefaultFontSize,
                        new RgbaFloat(_fontSettings.DefaultTextColor),
                        _fontSettings.DefaultOutlineColor?.ToRgbaFloat()
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
            var span = new Range(firstLine.GlyphSpan.Start, lastLine.GlyphSpan.End);
            _glyphRun = GetGlyphRun(span);
            ctx.RenderContext.Text.RequestGlyphs(_textLayout, _glyphRun);
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            var offset = new Vector2(0, - _range.start * _lineHeight);

            Vector3 pos = Transform.Position;
            var scissorRect = new RectangleU(
                (uint)pos.X,
                (uint)pos.Y,
                _textLayout.MaxBounds.Width,
                _visibleHeight
            );
            ctx.Text.Render(
                ctx,
                batch,
                _textLayout,
                _glyphRun,
                WorldMatrix,
                offset,
                scissorRect,
                Color.A
            );
        }

        private GlyphRun GetGlyphRun(Range span) => new()
        {
            Font = _fontSettings.DefaultFont,
            FontSize = _fontSettings.DefaultFontSize,
            Color = new RgbaFloat(_fontSettings.DefaultTextColor),
            OutlineColor = _fontSettings.DefaultOutlineColor?.ToRgbaFloat() ?? RgbaFloat.White,
            GlyphSpan = span,
            Flags = GlyphRunFlags.Outline
        };

        public override Size GetUnconstrainedBounds(RenderContext ctx)
        {
            return _textLayout.BoundingBox.Size.ToSize();
        }
    }
}
