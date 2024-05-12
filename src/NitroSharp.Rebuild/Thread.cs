using System;
using System.Diagnostics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

namespace NitroSharp;

internal sealed class Thread : Entity, IVmThread
{
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

    internal record struct WaitOperation(WaitCondition WaitCondition, EntityQuery? EntityQuery, TimeSpan? Deadline)
    {
        public static WaitOperation Suspend(TimeSpan deadline) => new(WaitCondition.Timeout, null, deadline);
        public static WaitOperation UserInput(TimeSpan? deadline) => new(WaitCondition.UserInput, null, deadline);

    }

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
        if (!HasWaitExpired(ctx)) { return; }

        _waitOperation = null;
        ctx.VM.Tick(ref _vmState, ctx.Builtins);
    }

    public void Wait(WaitOperation waitOperation)
    {
        _waitOperation = waitOperation;
    }

    private bool HasWaitExpired(GameContext ctx)
    {
        Stopwatch clock = ctx.Clock;
        if (_waitOperation is { } waitOperation)
        {
            if (waitOperation.Deadline is { } deadline)
            {
                if (clock.Elapsed >= deadline) { return true; }
            }

            InputContext input = ctx.InputContext;
            return waitOperation switch
            {
                { WaitCondition: WaitCondition.UserInput } => input.VKeyDown(VirtualKey.Advance),
                _ => true
            };
        }

        return true;
    }
}
