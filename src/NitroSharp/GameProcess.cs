using System;
using System.Collections.Generic;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal enum WaitCondition
    {
        None,
        UserInput,
        MoveCompleted,
        ZoomCompleted,
        RotateCompleted,
        FadeCompleted,
        BezierMoveCompleted,
        TransitionCompleted,
        EntityIdle,
        FrameReady,
        LineRead
    }

    internal readonly struct WaitOperation
    {
        public readonly NsScriptThread Thread;
        public readonly WaitCondition Condition;
        public readonly EntityQuery? EntityQuery;
        public readonly Texture? ScreenshotTexture;

        public WaitOperation(
            NsScriptThread thread,
            WaitCondition condition,
            EntityQuery? entityQuery,
            Texture? screenshotTexture = null)
        {
            Thread = thread;
            Condition = condition;
            EntityQuery = entityQuery;
            ScreenshotTexture = screenshotTexture;
        }

        public void Deconstruct(out WaitCondition condition, out EntityQuery? query)
        {
            condition = Condition;
            query = EntityQuery;
        }
    }

    internal sealed class GameProcess
    {
        private readonly Queue<WaitOperation> _waitOperations;
        private readonly List<WaitOperation> _survivedWaits;

        public GameProcess(NsScriptProcess vmProcess, FontConfiguration fontConfig)
        {
            VmProcess = vmProcess;
            World = new World();
            _waitOperations = new Queue<WaitOperation>();
            _survivedWaits = new List<WaitOperation>();
            FontConfig = fontConfig;
        }

        public NsScriptProcess VmProcess { get; }
        public World World { get; }
        public FontConfiguration FontConfig { get; }

        public void Wait(
            NsScriptThread thread,
            WaitCondition condition,
            TimeSpan? timeout = null,
            EntityQuery? entityQuery = null,
            Texture? screenshotTexture = null)
        {
            VmProcess.VM.SuspendThread(thread, timeout);
            if (condition != WaitCondition.None)
            {
                _waitOperations.Enqueue(
                    new WaitOperation(thread, condition, entityQuery, screenshotTexture)
                );
            }
        }

        public void ProcessWaitOperations(GameContext ctx)
        {
            Queue<WaitOperation> waits = _waitOperations;
            while (waits.TryDequeue(out WaitOperation wait))
            {
                if (wait.Thread.IsActive) { continue; }
                if (ShouldResume(wait, ctx.InputContext))
                {
                    VmProcess.VM.ResumeThread(wait.Thread);
                    if (wait.ScreenshotTexture is Texture screenshotTexture)
                    {
                        ctx.RenderContext.CaptureFramebuffer(screenshotTexture);
                    }
                }
                else
                {
                    _survivedWaits.Add(wait);
                }
            }

            foreach (WaitOperation wait in _survivedWaits)
            {
                waits.Enqueue(wait);
            }

            _survivedWaits.Clear();
        }

        public void Dispose()
        {
            VmProcess.Terminate();
            World.Dispose();
        }

        private bool ShouldResume(in WaitOperation wait, InputContext input)
        {
            uint contextId = wait.Thread.Id;

            bool checkInput() => input.VKeyDown(VirtualKey.Advance);

            bool checkIdle(EntityQuery query)
            {
                foreach (Entity entity in World.Query(contextId, query))
                {
                    if (!entity.IsIdle) { return false; }
                }

                return true;
            }

            bool checkAnim(EntityQuery query, AnimationKind anim)
            {
                foreach (RenderItem entity in World.Query<RenderItem>(contextId, query))
                {
                    if (entity.IsAnimationActive(anim)) { return false; }
                }

                return true;
            }

            bool checkLineRead(EntityQuery query)
            {
                foreach (DialoguePage page in World.Query<DialoguePage>(contextId, query))
                {
                    if (page.LineRead) { return true; }
                }

                return false;
            }

            return wait switch
            {
                (WaitCondition.UserInput, _) => checkInput(),
                (WaitCondition.EntityIdle, { } query) => checkIdle(query),
                (WaitCondition.FadeCompleted, { } query) => checkAnim(query, AnimationKind.Fade),
                (WaitCondition.MoveCompleted, { } query) => checkAnim(query, AnimationKind.Move),
                (WaitCondition.ZoomCompleted, { } query) => checkAnim(query, AnimationKind.Zoom),
                (WaitCondition.RotateCompleted, { } query) => checkAnim(query, AnimationKind.Rotate),
                (WaitCondition.BezierMoveCompleted, { } query) => checkAnim(query, AnimationKind.BezierMove),
                (WaitCondition.TransitionCompleted, { } query) => checkAnim(query, AnimationKind.Transition),
                (WaitCondition.FrameReady, _) => true,
                (WaitCondition.LineRead, { } query) => checkLineRead(query),
                _ => false
            };
        }
    }
}
