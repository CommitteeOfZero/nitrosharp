using NitroSharp.NsScript.VM;

namespace NitroSharp
{
    internal sealed class VmThread : Entity
    {
        public VmThread(in ResolvedEntityPath path, ThreadContext context)
            : base(path)
        {
            Context = context;
        }

        public ThreadContext Context { get; }
        public override bool IsIdle => Context.DoneExecuting;
    }
}
