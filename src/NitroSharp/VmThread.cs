using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp
{
    internal sealed class VmThread : Entity
    {
        private readonly NsScriptVM _vm;

        public VmThread(in ResolvedEntityPath path, NsScriptVM vm, NsScriptProcess process, string target)
            : base(path)
        {
            _vm = vm;
            Target = target;
            Thread = vm.CreateThread(process, Target);
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
            Target = saveData.Target;
            Thread = process.GetThread(saveData.ThreadId);
        }

        public string Target { get; }
        public NsScriptThread Thread { get; private set; }
        public override bool IsIdle => !Thread.IsActive || Thread.DoneExecuting;

        public override EntityKind Kind => EntityKind.VmThread;

        public void Restart()
        {
            if (!Thread.DoneExecuting)
            {
                _vm.TerminateThread(Thread);
            }
            Thread = _vm.CreateThread(Thread.Process, Target);
            _vm.ResumeThread(Thread);
        }

        public void Suspend() => _vm.SuspendThread(Thread);
        public void Resume() => _vm.ResumeThread(Thread);
        public void Terminate() => _vm.TerminateThread(Thread);

        public override void Dispose()
        {
            Terminate();
        }

        public new VmThreadSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            ThreadId = Thread.Id,
            Target = Target
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
