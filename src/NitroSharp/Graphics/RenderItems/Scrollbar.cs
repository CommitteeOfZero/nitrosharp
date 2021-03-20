using System;
using System.Numerics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp.Graphics
{
    internal sealed class Scrollbar : RenderItem2D, UiElement
    {
        private enum State
        {
            Normal,
            Hovered,
            Held
        }

        private readonly SpriteTexture _knob;
        private readonly Vector2 _p1;
        private readonly Vector2 _p2;
        private readonly float _length;
        private readonly Vector2Component _axis;
        private readonly float _min;
        private readonly float _max;

        private State _state;
        private UiElementFocusData _focusData;
        private Vector2 _mousePos;

        public Scrollbar(
            in ResolvedEntityPath path,
            int priority,
            NsScrollDirection scrollDirection,
            SpriteTexture knob,
            Vector2 p1, Vector2 p2,
            float initialValue)
            : base(path, priority)
        {
            _knob = knob;
            _p1 = p1;
            _p2 = p2;
            _state = State.Normal;
            _axis = scrollDirection == NsScrollDirection.Vertical
                ? Vector2Component.Y
                : Vector2Component.X;
            _length = MathF.Abs(p1.Get(_axis) - p2.Get(_axis));
            float d = initialValue * _length;
            float val = p1.Get(_axis) > p2.Get(_axis) ? p1.Get(_axis) - d : p1.Get(_axis) + d;
            Transform.Position = scrollDirection == NsScrollDirection.Vertical
                ? new Vector3(p1.X, val, 0)
                : new Vector3(val, p1.Y, 0);
            _min = MathF.Min(p1.Get(_axis), p2.Get(_axis));
            _max = MathF.Max(p1.Get(_axis), p2.Get(_axis));
        }

        public override EntityKind Kind => EntityKind.Scrollbar;

        public RenderItem2D? RenderItem => this;
        public bool CanFocus => true;
        public ref UiElementFocusData FocusData => ref _focusData;
        public bool IsFocused => _state != State.Normal;

        public float GetValue()
        {
            Vector2 pos = Transform.Position.XY();
            float d = MathF.Abs(_p1.Get(_axis) - pos.Get(_axis));
            return d / _length;
        }

        public bool HandleEvents(GameContext ctx)
        {
            InputContext inputCtx = ctx.InputContext;
            _mousePos = inputCtx.MousePosition;
            bool hovered = HitTest(ctx.RenderContext, inputCtx);
            bool pressed = inputCtx.VKeyState(VirtualKey.Enter);
            _state = (hovered, pressed, _state) switch
            {
                (true, false, _) => State.Hovered,
                (true, true, _) => State.Held,
                (false, true, State.Held) => State.Held,
                _ => State.Normal
            };

            bool held = _state == State.Held;
            if (held)
            {
                float value = MathUtil.Clamp(_mousePos.Get(_axis), _min, _max);
                //if (_p1.Get(_axis) > _p2.Get(_axis))
                //{
                //    value = _p1.Get(_axis) - value;
                //}
                Transform.Position = _axis == Vector2Component.Y
                    ? new Vector3(Transform.Position.X, value, 0)
                    : new Vector3(value, Transform.Position.Y, 0);
            }
            return held;
        }

        protected override void Render(RenderContext ctx, DrawBatch batch)
        {
            batch.PushQuad(
                Quad,
                _knob.Resolve(ctx),
                ctx.WhiteTexture,
                default,
                BlendMode,
                FilterMode
            );
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => _knob.GetSize(ctx);

        public override void Dispose()
        {
            base.Dispose();
            _knob.Dispose();
        }
    }
}
