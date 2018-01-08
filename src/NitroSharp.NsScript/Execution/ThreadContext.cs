using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Execution
{
    public sealed class ThreadContext
    {
        private readonly Stack<Frame> _callstack;

        internal ThreadContext(string name, MergedSourceFileSymbol module, InvocableSymbol entryPoint)
        {
            Name = name;
            EntryPoint = entryPoint;
            _callstack = new Stack<Frame>();
            _callstack.Push(new Frame(module, entryPoint));
        }

        public string Name { get; }
        public InvocableSymbol EntryPoint { get; }

        internal Frame CurrentFrame => _callstack.Peek();
        internal Statement PC => CurrentFrame.CurrentStatement;

        public bool IsSuspended { get; internal set; }
        public TimeSpan SleepTimeout { get; internal set; }
        public TimeSpan SuspensionTime { get; internal set; }

        public bool DoneExecuting => _callstack.Count == 0;

        internal void PopFrame() => _callstack.Pop();
        internal void PushFrame(Frame frame)
        {
            _callstack.Push(frame);
        }

        internal bool Advance()
        {
            if (!DoneExecuting)
            {
                if (!CurrentFrame.IsEmpty && CurrentFrame.Advance())
                {
                    return true;
                }

                while (!DoneExecuting && CurrentFrame.IsEmpty)
                {
                    _callstack.Pop();
                }
            }

            return !DoneExecuting;
        }
    }
}
