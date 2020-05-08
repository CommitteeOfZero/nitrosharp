using System;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class Builtins : BuiltInFunctions
    {
        private readonly World _world;
        private readonly Context _ctx;
        private readonly RenderContext _renderCtx;

        public Builtins(Context context)
        {
            _ctx = context;
            _renderCtx = context.RenderContext;
            _world = context.World;
        }

        public override void CreateChoice(in EntityPath entityPath)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new Choice(resolvedPath));
            }
        }

        public override void CreateEntity(in EntityPath path)
        {
        }

        public override void Request(EntityQuery query, NsEntityAction action)
        {
            foreach (Entity entity in _world.Query(query))
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
                        _ctx.VM.ResumeThread(thread.Context);
                        break;
                    case (VmThread thread, NsEntityAction.Resume):
                        _ctx.VM.ResumeThread(thread.Context);
                        break;
                    case (VmThread thread, NsEntityAction.Pause):
                        _ctx.VM.SuspendThread(thread.Context);
                        break;
                    case (VmThread thread, NsEntityAction.Stop):
                        _ctx.VM.TerminateThread(thread.Context);
                        break;
                    case (_, NsEntityAction.Enable):
                        _world.EnableEntity(entity);
                        break;
                    case (_, NsEntityAction.Disable):
                        _world.DisableEntity(entity);
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
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.SetAlias(resolvedPath.Id, alias);
            }
        }

        public override void DestroyEntities(EntityQuery query)
        {
            foreach (Entity entity in _world.Query(query))
            {
                if (!entity.IsLocked)
                {
                    _world.DestroyEntity(entity);
                }
            }
        }

        public override void CreateThread(in EntityPath entityPath, string target)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                ThreadContext context = _ctx.VM.CreateThread(resolvedPath.Id.Path, target);
                _world.Add(new VmThread(resolvedPath, context));
            }
        }

        public override void Delay(TimeSpan delay)
        {
            if (delay > TimeSpan.Zero)
            {
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
            _ctx.Wait(CurrentThread, WaitCondition.UserInput, timeout);
        }
    }
}
