using System;
using System.Diagnostics;
using NitroSharp.Input;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

namespace NitroSharp;

internal sealed class Thread : Entity, IVmThread
{
    private NsScriptThreadState _vmState;
    private WaitOperation? _waitOperation;

    public Thread(EntityName name, Entity? parent, NsScriptThreadState vmState, bool isMain)
        : base(name, parent)
    {
        _vmState = vmState;
        IsMain = isMain;
    }

    public bool IsMain { get; }
    public ref NsScriptThreadState State => ref _vmState;

    public override void Update(GameContext ctx)
    {
        if (HasWaitExpired(ctx))
        {
            _waitOperation = null;
            ctx.VM.Tick(ref _vmState, ctx.Builtins);
        }
    }

    public void Wait(WaitOperation waitOperation)
    {
        Debug.Assert(_waitOperation is null, "Thread is already suspended.");
        _waitOperation = waitOperation;
    }

    private bool HasWaitExpired(GameContext ctx)
    {
        Stopwatch clock = ctx.Clock;
        if (_waitOperation is not { } waitOperation) { return true; }
        if (waitOperation.Deadline is { } deadline && clock.Elapsed >= deadline)
        {
            return true;
        }

        InputContext input = ctx.InputContext;
        return waitOperation switch
        {
            { Condition: WaitCondition.UserInput } => input.Consume(InputAction.Advance),
            {
                Condition: WaitCondition.MoveCompleted or WaitCondition.FadeCompleted or WaitCondition.ZoomCompleted,
                EntityQuery: { } query
            } => hasAnimationCompleted(query, waitOperation.Condition),
            _ => false
        };

        bool hasAnimationCompleted(in EntityQuery query, WaitCondition condition)
        {
            AnimationKind animationKind = condition switch
            {
                WaitCondition.FadeCompleted => AnimationKind.Fade,
                WaitCondition.ZoomCompleted => AnimationKind.Zoom,
                WaitCondition.MoveCompleted => AnimationKind.Move,
                _ => throw ThrowHelper.UnexpectedValueOf<WaitCondition>()
            };

            foreach (Entity entity in World.Query(query))
            {
                if (entity.IsAnimationActive(animationKind)) { return false; }
            }

            return true;
        }
    }

    internal enum WaitCondition
    {
        Timeout,
        UserInput,
        MoveCompleted,
        ZoomCompleted,
        RotateCompleted,
        FadeCompleted,
        BezierMoveCompleted,
        TransitionCompleted,
        EntityIdle,
    }

    internal record struct WaitOperation(WaitCondition Condition, EntityQuery? EntityQuery, TimeSpan? Deadline)
    {
        public static WaitOperation Suspend(TimeSpan deadline) => new(WaitCondition.Timeout, null, deadline);
        public static WaitOperation UserInput(TimeSpan? deadline) => new(WaitCondition.UserInput, null, deadline);

    }
}
