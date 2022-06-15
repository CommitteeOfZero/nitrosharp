using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;

namespace NitroSharp
{
    internal sealed class VmThread : Entity
    {
        private readonly NsScriptVM _vm;
        private readonly NsScriptProcess _process;

        public VmThread(
            in ResolvedEntityPath path,
            string module,
            string target,
            NsScriptVM vm,
            NsScriptProcess process) : base(path)
        {
            _vm = vm;
            _process = process;
            Module = module;
            Target = target;
            Thread = null;
            if (Parent is Choice choice)
            {
                switch (Id.MouseState)
                {
                    case MouseState.Over:
                        choice.MouseEnterThread = this;
                        break;
                    case MouseState.Leave:
                        choice.MouseLeaveThread = this;
                        break;
                }
            }
        }

        public VmThread(
            in ResolvedEntityPath path,
            in VmThreadSaveData saveData,
            NsScriptVM vm,
            NsScriptProcess process)
            : base(path, saveData.Common)
        {
            _vm = vm;
            _process = process;
            Module = saveData.Module;
            Target = saveData.Target;
            Thread = null;
            if (saveData.ThreadId is uint threadId)
            {
                Thread = process.GetThread(threadId);
            }
        }

        public string Module { get; }
        public string Target { get; }
        public NsScriptThread? Thread { get; private set; }
        public override bool IsIdle
        {
            get
            {
                if (Thread is NsScriptThread thread)
                {
                    return Thread.DoneExecuting;
                }
                return true;
            }
        }

        public override EntityKind Kind => EntityKind.VmThread;

        public void Start()
        {
            if (Thread is NsScriptThread thread && !thread.DoneExecuting)
            {
                _vm.ResumeThread(thread);
                return;
            }
            Thread = _vm.CreateThread(_process, Module, Target, start: true);
        }

        public void Restart()
        {
            if (Thread is NsScriptThread thread && !thread.DoneExecuting)
            {
                _vm.TerminateThread(thread);
            }
            Thread = _vm.CreateThread(_process, Module, Target, start: true);
        }

        public void Suspend()
        {
            if (Thread is NsScriptThread thread)
            {
                _vm.SuspendThread(thread);
            }
        }

        public void Resume()
        {
            if (Thread is NsScriptThread thread)
            {
                _vm.ResumeThread(thread);
            }
        }

        public void Terminate()
        {
            if (Thread is NsScriptThread thread)
            {
                _vm.TerminateThread(thread);
                Thread = null;
            }
        }

        public override void Dispose()
        {
            Terminate();
        }

        public new VmThreadSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            ThreadId = Thread?.Id,
            Module = Module,
            Target = Target
        };
    }

    [Persistable]
    internal readonly partial struct VmThreadSaveData : IEntitySaveData
    {
        public EntitySaveData Common { get; init; }
        public uint? ThreadId { get; init; }
        public string Module { get; init; }
        public string Target { get; init; }

        public EntitySaveData CommonEntityData => Common;
    }
}
