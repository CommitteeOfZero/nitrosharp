using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    public sealed class ThreadContext
    {
        internal ValueStack<CallFrame> CallFrameStack;
        internal ValueStack<ConstantValue> EvalStack;
        internal long? SuspensionTime;
        internal long? SleepTimeout;
        internal bool Yielded;
        internal EntityPath? DialoguePage;
        internal bool SelectResult;

        internal ThreadContext(uint id, ref CallFrame frame)
        {
            Id = id;
            CallFrameStack = new ValueStack<CallFrame>(4);
            CallFrameStack.Push(ref frame);
            EvalStack = new ValueStack<ConstantValue>(8);
            EntryModule = frame.Module.Name;
        }

        public uint Id { get; }
        public string EntryModule { get; }
        public bool DoneExecuting => CallFrameStack.Count == 0;
        public bool IsActive => SuspensionTime == null;

        internal ref CallFrame CurrentFrame => ref CallFrameStack.Peek();
    }

    internal struct CallFrame
    {
        public readonly NsxModule Module;
        public readonly ushort SubroutineIndex;
        public readonly SubroutineKind SubroutineKind;
        public readonly ushort ArgStart;
        public readonly ushort ArgCount;

        public int ProgramCounter;

        public CallFrame(
            NsxModule module,
            ushort subroutineIndex,
            SubroutineKind subroutineKind,
            int pc = 0,
            ushort argStart = 0,
            ushort argCount = 0)
        {
            Module = module;
            SubroutineIndex = subroutineIndex;
            SubroutineKind = subroutineKind;
            ProgramCounter = pc;
            ArgStart = argStart;
            ArgCount = argCount;
        }
    }
}
