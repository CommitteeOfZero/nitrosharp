using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    internal sealed class ThreadContext
    {
        private readonly VariableTable _globals;
        private readonly Stack<Frame> _frameStack;

        public ThreadContext(uint id, Module module, Statement target, VariableTable globals)
        {
            Id = id;
            CurrentModule = module;
            _globals = globals;
            _frameStack = new Stack<Frame>();

            PushContinuation(target);
        }

        public uint Id { get; }
        public Module CurrentModule { get; }
        public Frame CurrentFrame => _frameStack.Peek();   

        public SyntaxNode CurrentNode => CurrentFrame.Statements[CurrentFrame.Position];
        public bool Suspended { get; private set; }
        public bool DoneExecuting => _frameStack.Count == 0;

        public TimeSpan SleepTimeout { get; private set; }
        public TimeSpan SleepCounter { get; set; }

        public void Advance()
        {
            if (!DoneExecuting)
            {
                var frame = CurrentFrame;
                if (frame.Position < frame.Statements.Length - 1)
                {
                    frame.Position++;
                }
                else
                {
                    _frameStack.Pop();
                }
            }
        }

        public void Suspend()
        {
            Suspended = true;
        }

        public void Suspend(TimeSpan timeout)
        {
            Suspended = true;
            SleepTimeout = timeout;
            SleepCounter = TimeSpan.Zero;
        }

        public void Resume()
        {
            SleepTimeout = TimeSpan.Zero;
            Suspended = false;
        }

        public void PushContinuation(ImmutableArray<Statement> statements, bool advance = true)
        {
            if (advance)
            {
                Advance();
            }

            var frame = new Frame(statements, _globals);
            _frameStack.Push(frame);
        }

        public void PushContinuation(Statement statement, bool advance = true)
        {
            var block = statement as IBlock;
            var array = block?.Statements ?? ImmutableArray.Create(statement);
            PushContinuation(array, advance);
        }

        public void PushContinuation(Statement statement, VariableTable arguments, bool advance = true)
        {
            PushContinuation(statement, advance);
            CurrentFrame.Arguments = arguments;
        }
    }
}
