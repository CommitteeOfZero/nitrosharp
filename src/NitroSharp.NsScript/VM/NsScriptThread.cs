﻿using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    public sealed class NsScriptThread
    {
        internal ValueStack<CallFrame> CallFrameStack;
        internal ValueStack<ConstantValue> EvalStack;
        internal long? SuspensionTime;
        internal long? SleepTimeout;
        internal bool Yielded;
        internal EntityPath? DialoguePage;
        internal bool SelectResult;
        internal NsScriptThread? WaitingThread;

        internal NsScriptThread(uint id, ref CallFrame frame)
        {
            Id = id;
            CallFrameStack = new ValueStack<CallFrame>(4);
            CallFrameStack.Push(ref frame);
            EvalStack = new ValueStack<ConstantValue>(8);
            EntryModule = frame.Module.Name;
            Process = null!;
        }

        public NsScriptProcess Process { get; internal set; }
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

        public int ProgramCounter;

        public CallFrame(NsxModule module, ushort subroutineIndex, int pc)
        {
            Module = module;
            SubroutineIndex = subroutineIndex;
            ProgramCounter = pc;
        }
    }
}