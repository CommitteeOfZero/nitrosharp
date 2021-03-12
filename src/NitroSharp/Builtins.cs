using System;
using System.IO;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal sealed partial class Builtins : BuiltInFunctions
    {
        private readonly GameContext _ctx;
        private readonly RenderContext _renderCtx;

        public Builtins(GameContext context)
        {
            _ctx = context;
            _renderCtx = context.RenderContext;
        }

        private World World => _ctx.ActiveProcess.World;

        private Entity? Get(in EntityPath entityPath)
            => World.Get(CurrentThread.DeclaredId, entityPath);

        private SmallList<Entity> Query(EntityQuery query)
        {
            SmallList<Entity> results = World.Query(CurrentThread.DeclaredId, query);
            if (results.Count == 0)
            {
                EmptyResults(query);
            }
            return results;
        }

        private QueryResultsEnumerable<T> Query<T>(EntityQuery query) where T : Entity
        {
            QueryResultsEnumerable<T> results = World.Query<T>(CurrentThread.DeclaredId, query);
            if (results.IsEmpty)
            {
                EmptyResults(query);
            }
            return results;
        }

        private void EmptyResults(EntityQuery query) { }

        private bool ResolvePath(in EntityPath path, out ResolvedEntityPath resolvedPath)
        {
            return World.ResolvePath(CurrentThread.DeclaredId, path, out resolvedPath);
        }

        public override void Exit()
        {
            _ctx.ShutdownSignal.Cancel();
        }

        public override ConstantValue FormatString(string format, object[] args)
        {
            return ConstantValue.String(SprintfNET.StringFormatter.PrintF(format, args));
        }

        public override void CreateEntity(in EntityPath path)
        {
            if (ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new BasicEntity(resolvedPath));
            }
        }

        public override void Request(EntityQuery query, NsEntityAction action)
        {
            foreach (Entity entity in Query(query))
            {
                switch (entity, action)
                {
                    case (RenderItem2D ri, NsEntityAction.SetAdditiveBlend):
                        ri.BlendMode = BlendMode.Additive;
                        break;
                    case (RenderItem2D ri, NsEntityAction.SetReverseSubtractiveBlend):
                        ri.BlendMode = BlendMode.ReverseSubtractive;
                        break;
                    case (RenderItem2D ri, NsEntityAction.SetMultiplicativeBlend):
                        ri.BlendMode = BlendMode.Multiplicative;
                        break;
                    case (RenderItem2D ri, NsEntityAction.EnableFiltering):
                        ri.FilterMode = FilterMode.Linear;
                        break;
                    case (VmThread thread, NsEntityAction.Start):
                        thread.Restart();
                        break;
                    case (VmThread thread, NsEntityAction.Resume):
                        thread.Resume();
                        break;
                    case (VmThread thread, NsEntityAction.Pause):
                        thread.Suspend();
                        break;
                    case (VmThread thread, NsEntityAction.Stop):
                        thread.Suspend();
                        break;
                    case (RenderItem ri, NsEntityAction.Enable):
                        if (ri.Parent is Choice)
                        {
                            ri.Reveal();
                        }
                        else
                        {
                            World.EnableEntity(ri);
                        }
                        break;
                    case (RenderItem ri, NsEntityAction.Disable):
                        if (ri.Parent is Choice)
                        {
                            ri.Hide();
                        }
                        else
                        {
                            World.DisableEntity(ri);
                        }
                        break;
                    case (Video video, NsEntityAction.Play):
                        video.Stream.Start();
                        break;
                    case (Video video, NsEntityAction.Pause):
                        video.Stream.Pause();
                        break;
                    case (Video video, NsEntityAction.Resume):
                        video.Stream.Resume();
                        break;
                    case (Sound sound, NsEntityAction.Play):
                        sound.Play();
                        break;
                    case (DialoguePage page, NsEntityAction.NoTextAnimation):
                        page.DisableAnimation = true;
                        break;
                    case (_, NsEntityAction.Enable):
                        World.EnableEntity(entity);
                        break;
                    case (_, NsEntityAction.Disable):
                        World.DisableEntity(entity);
                        break;
                    case (_, NsEntityAction.DestroyWhenIdle):
                        // Demo 5 HACK
                        //if (entity is DialoguePage)
                        {
                            World.DestroyWhenIdle(entity);
                        }
                        break;
                    case (_, NsEntityAction.Lock):
                        entity.Lock();
                        break;
                    case (_, NsEntityAction.Unlock):
                        entity.Unlock();
                        break;
                }
            }
        }

        public override void SetAlias(in EntityPath entityPath, in EntityPath alias)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.SetAlias(resolvedPath.Id, alias);
            }
        }

        public override void DestroyEntities(EntityQuery query)
        {
            foreach (Entity entity in Query(query))
            {
                if (!entity.IsLocked)
                {
                    World.DestroyEntity(entity);
                }
            }
        }

        public override void CreateThread(in EntityPath entityPath, string target)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new VmThread(resolvedPath, _ctx.VM, CurrentProcess, target));
            }
        }

        public override void Delay(TimeSpan delay)
        {
            if (delay > TimeSpan.Zero)
            {
                delay = AdjustDuration(delay);
                _ctx.Wait(CurrentThread, WaitCondition.None, delay);
            }
        }

        public override void WaitAction(EntityQuery query, TimeSpan? timeout)
        {
            _ctx.Wait(CurrentThread, WaitCondition.EntityIdle, timeout, query);
        }

        public override void WaitMove(EntityQuery query)
        {
            _ctx.Wait(CurrentThread, WaitCondition.MoveCompleted, timeout: null, query);
        }

        public override void WaitForInput()
        {
            _ctx.Wait(CurrentThread, WaitCondition.UserInput);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            timeout = AdjustDuration(timeout);
            _ctx.Wait(CurrentThread, WaitCondition.UserInput, timeout);
        }

        public override void MoveCursor(int x, int y)
        {
            _ctx.Window.SetMousePosition(new Vector2(x, y));
        }

        public override bool MountSaveData(uint slot)
        {
            return true;
        }

        public override int GetSecondsElapsed()
        {
            return (int)_ctx.Clock.Elapsed.TotalSeconds;
        }

        public override bool SaveExists(uint slot)
        {
            return _ctx.SaveManager.SaveExists(slot);
        }

        public override bool FileExists(string path)
        {
            string fullPath = Path.Combine(_ctx.Config.ContentRoot, path);
            return File.Exists(fullPath);
        }

        public override void SaveGame(uint slot)
        {
            _ctx.Defer(DeferredOperation.SaveGame(slot));
        }

        public override void LoadGame(uint slot)
        {
            _ctx.Defer(DeferredOperation.LoadGame(slot));
        }

        public override bool X360_UserDataExists()
        {
            return _ctx.SaveManager.CommonSaveDataExists();
        }

        public override float X360_GetTriggerAxis(XboxTrigger trigger)
        {
            VirtualAxis axis = trigger switch
            {
                XboxTrigger.Left => VirtualAxis.TriggerLeft,
                XboxTrigger.Right => VirtualAxis.TriggerRight,
                _ => ThrowHelper.Unreachable<VirtualAxis>()
            };
            return _ctx.InputContext.GetAxis(axis);
        }
    }
}
