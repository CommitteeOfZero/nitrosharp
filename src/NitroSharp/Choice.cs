using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using Vulkan.Xlib;

#nullable enable

namespace NitroSharp
{
    internal sealed class Choice : Entity
    {
        private struct FocusData
        {
            public EntityId Left;
            public EntityId Up;
            public EntityId Right;
            public EntityId Down;
        }

        private readonly List<RenderItem2D> _mouseOverVisuals;
        private readonly List<RenderItem2D> _mouseDownVisuals;
        private FocusData _nextFocus;

        public Choice(in ResolvedEntityPath path)
            : base(in path)
        {
            _mouseOverVisuals = new List<RenderItem2D>();
            _mouseDownVisuals = new List<RenderItem2D>();
            LastMouseState = MouseState.Normal;
        }

        public RenderItem2D? DefaultVisual { get; set; }
        public VmThread? MouseEnterThread { get; set; }
        public VmThread? MouseLeaveThread { get; set; }

        public int Priority => DefaultVisual?.Key.Priority ?? 0;
        public bool CanFocus { get; private set; }

        public MouseState LastMouseState { get; private set; }
        private MouseState PrevMouseState { get; set; }

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

        public void HandleInput(InputContext inputCtx, RenderContext renderCtx)
        {
            if (DefaultVisual is RenderItem2D visual)
            {
                bool mouseDown = inputCtx.VKeyState(VirtualKey.Enter);
                bool mouseOver = visual.HitTest(renderCtx, inputCtx);
                MouseState newPrevMouseState = LastMouseState;
                LastMouseState = (PrevMouseState, mouseOver, mouseDown) switch
                {
                    (MouseState.Down, true, false) => MouseState.Clicked,
                    (_, true, false) => MouseState.Over,
                    (_, true, true) => MouseState.Down,
                    (_, false, _) => MouseState.Normal
                };
                PrevMouseState = newPrevMouseState;
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

        private enum StateTransition
        {
            None,
            Entered,
            Left,
            Pressed
        }

        public bool Update()
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

                StateTransition transition = (PrevMouseState, LastMouseState) switch
                {
                    (MouseState.Normal, MouseState.Over) => StateTransition.Entered,
                    (MouseState.Over, MouseState.Normal) => StateTransition.Left,
                    (MouseState.Over, MouseState.Down) => StateTransition.Pressed,
                    (MouseState.Down, MouseState.Normal) => StateTransition.Left,
                    _ => StateTransition.None
                };

                var duration = TimeSpan.FromMilliseconds(200);
                switch (transition)
                {
                    case StateTransition.Entered:
                        visual.Fade(0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 1.0f, TimeSpan.Zero);
                        MouseLeaveThread?.Terminate();
                        MouseEnterThread?.Restart();
                        break;
                    case StateTransition.Left:
                        fade(_mouseOverVisuals, 0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        visual.Fade(1.0f, duration);
                        MouseEnterThread?.Terminate();
                        MouseLeaveThread?.Restart();
                        break;
                    case StateTransition.Pressed:
                        visual.Fade(0, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseDownVisuals, 1.0f, TimeSpan.Zero);
                        break;
                }

                bool clicked = LastMouseState == MouseState.Clicked;
                if (clicked)
                {
                    DefaultVisual?.Fade(0, TimeSpan.Zero);
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
