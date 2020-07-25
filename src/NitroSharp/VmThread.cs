using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

namespace NitroSharp
{
    internal sealed class VmThread : Entity
    {
        private readonly NsScriptVM _vm;

        public VmThread(in ResolvedEntityPath path, NsScriptVM vm, string target)
            : base(path)
        {
            _vm = vm;
            Target = target;
            Context = vm.CreateThread(Target);
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

        public string Target { get; }
        public ThreadContext Context { get; private set; }
        public override bool IsIdle => Context.DoneExecuting;

        public void Restart()
        {
            if (!Context.DoneExecuting)
            {
                _vm.TerminateThread(Context);
            }
            Context = _vm.CreateThread(Target);
            _vm.ResumeThread(Context);
        }

        public void Suspend() => _vm.SuspendThread(Context);
        public void Resume() => _vm.ResumeThread(Context);
        public void Terminate() => _vm.TerminateThread(Context);

        public override void Dispose()
        {
            Terminate();
        }
    }
}
