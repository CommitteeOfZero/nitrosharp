using System;
using System.Collections.Generic;
using System.Linq;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;
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
        LineRead
    }

    internal readonly struct WaitOperation
    {
        public readonly NsScriptThread Thread;
        public readonly WaitCondition Condition;
        public readonly EntityQuery? EntityQuery;

        public WaitOperation(
            NsScriptThread thread,
            WaitCondition condition,
            EntityQuery? entityQuery)
        {
            Thread = thread;
            Condition = condition;
            EntityQuery = entityQuery;
        }

        public WaitOperation(NsScriptProcess vmProcess, in WaitOperationSaveData saveData)
        {
            Condition = saveData.WaitCondition;
            EntityQuery = null;
            if (saveData.EntityQuery is string entityQuery)
            {
                EntityQuery = new EntityQuery(entityQuery);
            }

            Thread = vmProcess.GetThread(saveData.ThreadId);
        }

        public void Deconstruct(out WaitCondition condition, out EntityQuery? query)
        {
            condition = Condition;
            query = EntityQuery;
        }

        public WaitOperationSaveData ToSaveData() => new()
        {
            ThreadId = Thread.Id,
            EntityQuery = EntityQuery?.Value,
            WaitCondition = Condition
        };
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

        public GameProcess(
            GameContext ctx,
            in GameProcessSaveData saveData,
            IReadOnlyList<Texture> standaloneTextures)
        {
            _waitOperations = new Queue<WaitOperation>();
            _survivedWaits = new List<WaitOperation>();
            FontConfig = saveData.FontConfig;

            VmProcess = ctx.VM.RestoreProcess(saveData.VmProcessDump);

            var loadingCtx = new GameLoadingContext
            {
                Process = this,
                StandaloneTextures = standaloneTextures,
                Rendering = ctx.RenderContext,
                Content = ctx.Content,
                VM = ctx.VM,
                Backlog = ctx.Backlog
            };

            World = World.Load(saveData.World, loadingCtx);
            foreach (WaitOperationSaveData waitOp in saveData.WaitOperations)
            {
                _waitOperations.Enqueue(new WaitOperation(VmProcess, waitOp));
            }
        }

        public NsScriptProcess VmProcess { get; }
        public World World { get; }
        public FontConfiguration FontConfig { get; }

        public void Wait(
            NsScriptThread thread,
            WaitCondition condition,
            TimeSpan? timeout = null,
            EntityQuery? entityQuery = null)
        {
            VmProcess.VM.SuspendThread(thread, timeout);
            if (condition != WaitCondition.None)
            {
                _waitOperations.Enqueue(
                    new WaitOperation(thread, condition, entityQuery)
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

        public GameProcessSaveData Dump(GameSavingContext ctx) => new()
        {
            World = World.ToSaveData(ctx),
            WaitOperations = _waitOperations.Select(x => x.ToSaveData()).ToArray(),
            VmProcessDump = VmProcess.Dump(),
            FontConfig = FontConfig
        };

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
                (WaitCondition.LineRead, { } query) => checkLineRead(query),
                _ => false
            };
        }
    }

    [Persistable]
    internal readonly partial struct GameProcessSaveData
    {
        public NsScriptProcessDump VmProcessDump { get; init; }
        public WorldSaveData World { get; init; }
        public WaitOperationSaveData[] WaitOperations { get; init; }
        public FontConfiguration FontConfig { get; init; }
    }

    [Persistable]
    internal readonly partial struct WaitOperationSaveData
    {
        public uint ThreadId { get; init; }
        public WaitCondition WaitCondition { get; init; }
        public string? EntityQuery { get; init; }
    }
}
