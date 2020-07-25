using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NitroSharp.NsScript;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class Choice : Entity, UiElement
    {
        private struct FocusData
        {
            public EntityId Left;
            public EntityId Up;
            public EntityId Right;
            public EntityId Down;
        }

        private MouseState _mouseState;
        private MouseState _prevMouseState;
        private readonly List<RenderItem2D> _mouseOverVisuals;
        private readonly List<RenderItem2D> _mouseDownVisuals;
        private FocusData _nextFocus;

        public Choice(in ResolvedEntityPath path)
            : base(path)
        {
            _mouseOverVisuals = new List<RenderItem2D>();
            _mouseDownVisuals = new List<RenderItem2D>();
            _prevMouseState = _mouseState = MouseState.Normal;
        }

        public RenderItem2D? DefaultVisual { get; set; }
        public VmThread? MouseEnterThread { get; set; }
        public VmThread? MouseLeaveThread { get; set; }

        public int Priority => DefaultVisual?.Key.Priority ?? 0;
        public bool CanFocus { get; private set; }

        public bool IsHovered => _mouseState == MouseState.Over;
        public override bool IsIdle => true;

        public void AddMouseOver(RenderItem2D visual)
            => _mouseOverVisuals.Add(visual);

        public void AddMouseDown(RenderItem2D visual)
            => _mouseDownVisuals.Add(visual);

        public EntityId GetNextFocus(NsFocusDirection direction) => direction switch
        {
            NsFocusDirection.Left => _nextFocus.Left,
            NsFocusDirection.Up => _nextFocus.Up,
            NsFocusDirection.Right => _nextFocus.Right,
            NsFocusDirection.Down => _nextFocus.Down,
            _ => ThrowHelper.Unreachable<EntityId>()
        };

        public void RecordInput(InputContext inputCtx, RenderContext renderCtx)
        {
            if (DefaultVisual is RenderItem2D visual)
            {
                bool hovered = visual.HitTest(renderCtx, inputCtx);
                bool pressed = inputCtx.VKeyState(VirtualKey.Enter);
                MouseState newState = hovered switch
                {
                    true => (pressed, PrevMouseState: _prevMouseState) switch
                    {
                        (false, MouseState.Down) => MouseState.Clicked,
                        (false, _) => MouseState.Over,
                        (true, _) => MouseState.Down
                    },
                    _ => MouseState.Normal
                };
                _prevMouseState = _mouseState;
                _mouseState = newState;
            }
        }

        public void Focus(GameWindow window, RenderContext renderCtx)
        {
            if (DefaultVisual is RenderItem2D visual)
            {
                Size bounds = visual.GetUnconstrainedBounds(renderCtx);
                var center = new Vector2(bounds.Width / 2.0f, bounds.Height / 2.0f);
                window.SetMousePosition(visual.Transform.Position.XY() + center);
            }
        }

        public void SetNextFocus(NsFocusDirection direction, in EntityId entity)
        {
            switch (direction)
            {
                case NsFocusDirection.Left:
                    _nextFocus.Left = entity;
                    break;
                case NsFocusDirection.Up:
                    _nextFocus.Up = entity;
                    break;
                case NsFocusDirection.Right:
                    _nextFocus.Right = entity;
                    break;
                case NsFocusDirection.Down:
                    _nextFocus.Down = entity;
                    break;
            }
        }

        private enum Event
        {
            None,
            Entered,
            Left,
            Pressed
        }

        public bool HandleEvents()
        {
            static void fade(List<RenderItem2D> list, float dstOpacity, TimeSpan duration)
            {
                foreach (RenderItem2D ri in list)
                {
                    ri.Fade(dstOpacity, duration);
                }
            }

            if (DefaultVisual is RenderItem2D visual)
            {
                CanFocus = _mouseOverVisuals.Any(x => !x.IsHidden);

                Event evt = (PrevMouseState: _prevMouseState, MouseState: _mouseState) switch
                {
                    (MouseState.Normal, MouseState.Over) => Event.Entered,
                    (MouseState.Over, MouseState.Normal) => Event.Left,
                    (MouseState.Over, MouseState.Down) => Event.Pressed,
                    (MouseState.Down, MouseState.Normal) => Event.Left,
                    _ => Event.None
                };

                var duration = TimeSpan.FromMilliseconds(200);
                switch (evt)
                {
                    case Event.Entered:
                        visual.Fade(0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 1.0f, TimeSpan.Zero);
                        MouseLeaveThread?.Terminate();
                        MouseEnterThread?.Restart();
                        break;
                    case Event.Left:
                        fade(_mouseOverVisuals, 0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        visual.Fade(1.0f, duration);
                        MouseEnterThread?.Terminate();
                        MouseLeaveThread?.Restart();
                        break;
                    case Event.Pressed:
                        visual.Fade(0, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseDownVisuals, 1.0f, TimeSpan.Zero);
                        break;
                }

                bool clicked = _mouseState == MouseState.Clicked;
                if (clicked)
                {
                    DefaultVisual.Fade(0, TimeSpan.Zero);
                    fade(_mouseOverVisuals, 1.0f, TimeSpan.Zero);
                    fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                    MouseEnterThread?.Terminate();
                    MouseLeaveThread?.Terminate();
                }
                return clicked;
            }

            return false;
        }
    }
}
