using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;

namespace NitroSharp
{
    internal sealed class VmThread : Entity
    {
        private readonly NsScriptVM _vm;
        private readonly string _target;
        private NsScriptThread _thread;

        public VmThread(
            in ResolvedEntityPath path,
            string target,
            NsScriptVM vm,
            NsScriptThread thread) : base(path)
        {
            _vm = vm;
            _target = target;
            _thread = thread;
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
            _target = saveData.Target;
            _thread = process.GetThread(saveData.ThreadId);
        }

        public override bool IsIdle => !_thread.IsActive || _thread.DoneExecuting;

        public override EntityKind Kind => EntityKind.VmThread;

        public void Restart()
        {
            if (!_thread.DoneExecuting)
            {
                _vm.TerminateThread(_thread);
            }
            _thread = _vm.CreateThread(_thread.Process, _thread.EntryModule, _target, start: true)!;
        }

        public void Suspend()
        {
            _vm.SuspendThread(_thread);
        }

        public void Resume()
        {
            _vm.ResumeThread(_thread);
        }

        public void Terminate()
        {
            _vm.TerminateThread(_thread);
        }

        public override void Dispose()
        {
            Terminate();
        }

        public new VmThreadSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            ThreadId = _thread.Id,
            Target = _target
        };
    }

    [Persistable]
    internal readonly partial struct VmThreadSaveData : IEntitySaveData
    {
        public EntitySaveData Common { get; init; }
        public uint ThreadId { get; init; }
        public string Target { get; init; }

        public EntitySaveData CommonEntityData => Common;
    }
}
