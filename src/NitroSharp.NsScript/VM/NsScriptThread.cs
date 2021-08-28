using System;
using System.Diagnostics;
using System.Linq;
using NitroSharp.NsScript.Utilities;

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

        internal NsScriptThread(uint id, ref CallFrame frame, uint? declaredId = null)
        {
            Id = id;
            DeclaredId = declaredId ?? id;
            CallFrameStack = new ValueStack<CallFrame>(4);
            CallFrameStack.Push(ref frame);
            EvalStack = new ValueStack<ConstantValue>(8);
            EntryModule = frame.Module.Name;
            Process = null!;
        }

        internal NsScriptThread(NsScriptVM vm,  NsScriptProcess process, in NsScriptThreadDump dump)
        {
            CallFrame restoreCallFrame(in CallFrameDump dump)
            {
                NsxModule module = vm.GetModule(dump.ModuleName);
                return new CallFrame(module, dump.Subroutine, dump.PC);
            }

            static long? toTicksMaybe(double? ms)
            {
                if (ms is null) { return null; }
                return (long)Math.Round(Stopwatch.Frequency / 1000.0d * ms.Value);
            }

            Id = dump.Id;
            Process = process;
            DeclaredId = dump.DeclaredId;
            EntryModule = dump.EntryModule;
            SuspensionTime = toTicksMaybe(dump.SuspensionTimeMs);
            SleepTimeout = toTicksMaybe(dump.SleepTimeoutMs);

            EvalStack = new ValueStack<ConstantValue>(8);
            foreach (ConstantValue value in dump.EvalStack)
            {
                EvalStack.Push(value);
            }

            CallFrameStack = new ValueStack<CallFrame>(4);
            foreach (CallFrame callFrame in dump.CallStack.Select(x => restoreCallFrame(x)))
            {
                CallFrameStack.Push(callFrame);
            }

            if (dump.DialoguePage is string dialoguePage)
            {
                DialoguePage = new EntityPath(dialoguePage);
            }

            SelectResult = dump.SelectResult;
        }

        public NsScriptProcess Process { get; internal set; }
        public uint Id { get; }
        public uint DeclaredId { get; }
        public string EntryModule { get; }
        public bool DoneExecuting => CallFrameStack.Count == 0;
        public bool IsActive => SuspensionTime is null;

        internal ref CallFrame CurrentFrame => ref CallFrameStack.Peek();

        internal NsScriptThreadDump Dump()
        {
            static T[] toArray<T>(ref ValueStack<T> stack) where T : struct
                => stack.AsSpan().ToArray();

            static double? fromTicksMaybe(long? ticks)
            {
                if (ticks is null) { return null; }
                return ticks.Value / (double)Stopwatch.Frequency * 1000.0d;
            }

            return new NsScriptThreadDump
            {
                Id = Id,
                DeclaredId = DeclaredId,
                EntryModule = EntryModule,
                CallStack = toArray(ref CallFrameStack).Select(x => x.Dump()).ToArray(),
                EvalStack = toArray(ref EvalStack),
                SuspensionTimeMs = fromTicksMaybe(SuspensionTime),
                SleepTimeoutMs = fromTicksMaybe(SleepTimeout),
                DialoguePage = DialoguePage?.Value,
                SelectResult = SelectResult,
                WaitingThread = WaitingThread?.Id
            };
        }
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

        public CallFrameDump Dump() => new()
        {
            ModuleName = Module.Name,
            Subroutine = SubroutineIndex,
            PC = ProgramCounter
        };
    }

    [Persistable]
    internal readonly partial struct CallFrameDump
    {
        public string ModuleName { get; init; }
        public ushort Subroutine { get; init; }
        public int PC { get; init; }
    }

    [Persistable]
    internal readonly partial struct NsScriptThreadDump
    {
        public uint Id { get; init; }
        public uint DeclaredId { get; init; }
        public string EntryModule { get; init; }
        public CallFrameDump[] CallStack { get; init; }
        public ConstantValue[] EvalStack { get; init; }
        public double? SuspensionTimeMs { get; init; }
        public double? SleepTimeoutMs { get; init; }
        public string? DialoguePage { get; init; }
        public bool SelectResult { get; init; }
        public uint? WaitingThread { get; init; }
    }
}
