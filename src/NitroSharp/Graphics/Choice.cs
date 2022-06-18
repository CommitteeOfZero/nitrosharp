using System;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.Saving;

namespace NitroSharp.Graphics
{
    internal sealed class Choice : Entity, UiElement
    {
        private enum State
        {
            Normal,
            Focused,
            Pressed
        }

        private enum StateTransition
        {
            None,
            GotFocus,
            LostFocus,
            GotPressed
        }

        private UiElementFocusData _focusData;
        private State _state;
        private readonly List<RenderItem2D> _mouseOverVisuals;
        private readonly List<RenderItem2D> _mouseDownVisuals;

        public Choice(in ResolvedEntityPath path)
            : base(path)
        {
            _mouseOverVisuals = new List<RenderItem2D>();
            _mouseDownVisuals = new List<RenderItem2D>();
        }

        public Choice(in ResolvedEntityPath path, ChoiceSaveData saveData, World world)
            : base(path, saveData.CommonEntityData)
        {
            if (saveData.DefaultVisual.IsValid)
            {
                DefaultVisual = world.Get(saveData.DefaultVisual) as RenderItem2D;
            }

            _focusData = saveData.NextFocus;
            _mouseOverVisuals = new List<RenderItem2D>();
            _mouseDownVisuals = new List<RenderItem2D>();
            foreach (EntityId entityId in saveData.MouseOverVisuals)
            {
                if (world.Get(entityId) is RenderItem2D mouseOver)
                {
                    _mouseOverVisuals.Add(mouseOver);
                }
            }
            foreach (EntityId entityId in saveData.MouseDownVisuals)
            {
                if (world.Get(entityId) is RenderItem2D mouseDown)
                {
                    _mouseDownVisuals.Add(mouseDown);
                }
            }
        }

        public RenderItem2D? DefaultVisual { get; set; }
        public VmThread? MouseEnterThread { get; set; }
        public VmThread? MouseLeaveThread { get; set; }

        public RenderItem2D? RenderItem => DefaultVisual;
        public ref UiElementFocusData FocusData => ref _focusData;
        public bool IsFocused => _state == State.Focused;

        public override EntityKind Kind => EntityKind.Choice;
        public override bool IsIdle => true;

        public void AddMouseOver(RenderItem2D visual)
        {
            _mouseOverVisuals.Add(visual);
        }

        public void AddMouseDown(RenderItem2D visual)
        {
            _mouseDownVisuals.Add(visual);
        }

        public bool HandleEvents(GameContext ctx)
        {
            static void fade(List<RenderItem2D> list, float dstOpacity, TimeSpan duration)
            {
                foreach (RenderItem2D ri in list)
                {
                    ri.Fade(dstOpacity, duration);
                }
            }

            if (DefaultVisual is { } visual)
            {
                bool hovered = visual.HitTest(ctx.RenderContext, ctx.InputContext);
                bool pressed = ctx.InputContext.VKeyState(VirtualKey.Enter);
                State newState = (hovered, pressed) switch
                {
                    (true, true) => State.Pressed,
                    (true, false) => State.Focused,
                    _ => State.Normal
                };
                StateTransition transition = (_state, newState) switch
                {
                    (not State.Focused, State.Focused) => StateTransition.GotFocus,
                    (State.Focused, State.Normal) => StateTransition.LostFocus,
                    (State.Focused, State.Pressed) => StateTransition.GotPressed,
                    (State.Pressed, State.Normal) => StateTransition.LostFocus,
                    _ => StateTransition.None
                };
                _state = newState;

                var duration = TimeSpan.FromMilliseconds(200);
                switch (transition)
                {
                    case StateTransition.GotFocus:
                        visual.Fade(0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 1.0f, TimeSpan.Zero);
                        MouseLeaveThread?.Terminate();
                        MouseEnterThread?.Restart();
                        break;
                    case StateTransition.LostFocus:
                        fade(_mouseOverVisuals, 0.0f, duration);
                        fade(_mouseDownVisuals, 0.0f, TimeSpan.Zero);
                        visual.Fade(1.0f, duration);
                        MouseEnterThread?.Terminate();
                        MouseLeaveThread?.Restart();
                        break;
                    case StateTransition.GotPressed:
                        visual.Fade(0, TimeSpan.Zero);
                        fade(_mouseOverVisuals, 0.0f, TimeSpan.Zero);
                        fade(_mouseDownVisuals, 1.0f, TimeSpan.Zero);
                        MouseEnterThread?.Terminate();
                        MouseLeaveThread?.Terminate();
                        break;
                }

                return transition == StateTransition.GotPressed;
            }

            return false;
        }

        public new ChoiceSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            DefaultVisual = DefaultVisual?.Id ?? EntityId.Invalid,
            MouseOverVisuals = _mouseOverVisuals.Select(x => x.Id).ToArray(),
            MouseDownVisuals = _mouseDownVisuals.Select(x => x.Id).ToArray(),
            NextFocus = _focusData
        };
    }

    [Persistable]
    internal readonly partial struct ChoiceSaveData : IEntitySaveData
    {
        public EntitySaveData Common { get; init; }
        public EntityId DefaultVisual { get; init; }
        public EntityId[] MouseOverVisuals { get; init; }
        public EntityId[] MouseDownVisuals { get; init; }
        public UiElementFocusData NextFocus { get; init; }

        public EntitySaveData CommonEntityData => Common;
    }
}
