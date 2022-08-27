using System;
using NitroSharp.NsScript;

namespace NitroSharp.Graphics;

internal sealed class Choice : UiElement
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

    private readonly EntityId _entityId;
    private readonly EntityId _mouseUsualEntityId;
    private readonly EntityId _mouseOverEntityId;
    private readonly EntityId _mouseLeaveEntityId;
    private readonly EntityQuery _mouseOverQuery;
    private readonly EntityQuery _mouseClickQuery;

    private State _state;
    private UiElementFocusData _focusData;

    public Choice(EntityId entityId)
    {
        _entityId = entityId;
        _mouseUsualEntityId = entityId.Child(MouseStateEntities.MouseUsual);
        _mouseOverEntityId = entityId.Child(MouseStateEntities.MouseOver);
        _mouseLeaveEntityId = entityId.Child(MouseStateEntities.MouseLeave);
        _mouseOverQuery = new EntityQuery($"{entityId.Path}/{MouseStateEntities.MouseOver}/*");
        _mouseClickQuery = new EntityQuery($"{entityId.Path}/{MouseStateEntities.MouseClick}/*");
    }

    public EntityId Id => _entityId;
    public ref UiElementFocusData FocusData => ref _focusData;
    public bool IsFocused => _state == State.Focused;

    public RenderItem2D? TryGetMouseUsualVisual(World world)
        => world.Get(_mouseUsualEntityId)?.GetSingleChild<RenderItem2D>();

    public QueryResultsEnumerable<RenderItem2D> QueryMouseClickVisuals(World world)
        => world.Query<RenderItem2D>(_entityId.Context, _mouseClickQuery);

    public QueryResultsEnumerable<RenderItem2D> QueryMouseOverVisuals(World world)
        => world.Query<RenderItem2D>(_entityId.Context, _mouseOverQuery);

     public bool HandleEvents(GameContext ctx)
     {
        World world = ctx.ActiveProcess.World;
        if (TryGetMouseUsualVisual(world) is not { } visual)
        {
            return false;
        }

        QueryResultsEnumerable<RenderItem2D> mouseOverVisuals = QueryMouseOverVisuals(world);
        QueryResultsEnumerable<RenderItem2D> mouseClickVisuals = QueryMouseClickVisuals(world);
        VmThread? mouseOverThread = world.Get(_mouseOverEntityId)?.GetSingleChild<VmThread>();
        VmThread? mouseLeaveThread = world.Get(_mouseLeaveEntityId)?.GetSingleChild<VmThread>();

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
                fade(mouseClickVisuals, 0.0f, TimeSpan.Zero);
                fade(mouseOverVisuals, 1.0f, TimeSpan.Zero);
                mouseLeaveThread?.Terminate();
                mouseOverThread?.Restart();
                break;
            case StateTransition.LostFocus:
                fade(mouseOverVisuals, 0.0f, duration);
                fade(mouseClickVisuals, 0.0f, TimeSpan.Zero);
                visual.Fade(1.0f, duration);
                mouseOverThread?.Terminate();
                mouseLeaveThread?.Restart();
                break;
            case StateTransition.GotPressed:
                visual.Fade(0, TimeSpan.Zero);
                fade(mouseOverVisuals, 0.0f, TimeSpan.Zero);
                fade(mouseClickVisuals, 1.0f, TimeSpan.Zero);
                mouseOverThread?.Terminate();
                mouseLeaveThread?.Terminate();
                break;
        }

        return transition == StateTransition.GotPressed;

        static void fade(QueryResultsEnumerable<RenderItem2D> visuals, float dstOpacity, TimeSpan duration)
        {
            foreach (RenderItem2D renderItem in visuals)
            {
                renderItem.Fade(dstOpacity, duration);
            }
        }
    }
}
