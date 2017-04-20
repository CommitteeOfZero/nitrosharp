using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CommitteeOfZero.NsScript.Execution
{
    public sealed class ThreadContext
    {
        private readonly NsScriptInterpreter _interpreter;
        private readonly VariableTable _globals;
        internal readonly Stack<Frame> _frameStack;

        internal ThreadContext(uint id, NsScriptInterpreter interpreter, Module module, IJumpTarget target, VariableTable globals)
        {
            Id = id;
            _interpreter = interpreter;
            CurrentModule = module;
            _globals = globals;
            _frameStack = new Stack<Frame>();

            PushContinuation(target, target.Body);
        }

        public uint Id { get; }
        public Module CurrentModule { get; }
        public Frame CurrentFrame => _frameStack.Peek();   

        public SyntaxNode CurrentNode => CurrentFrame.Statements[CurrentFrame.Position];
        public bool Suspended { get; internal set; }
        public bool DoneExecuting => _frameStack.Count == 0;

        public TimeSpan SleepTimeout { get; internal set; }
        public TimeSpan SuspensionTime { get; internal set; }

        internal void Advance()
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
            _interpreter.SuspendThreadCore(this, TimeSpan.MaxValue);
        }

        public void Suspend(TimeSpan sleepTimeout)
        {
            _interpreter.SuspendThreadCore(this, sleepTimeout);
        }

        public void Resume()
        {
            _interpreter.ResumeThreadCore(this);
        }

        public void PushContinuation(IJumpTarget function, ImmutableArray<Statement> statements, bool advance = true)
        {
            Frame prevFrame = null;
            if (_frameStack.Count > 0)
            {
                prevFrame = CurrentFrame;
            }

            if (advance)
            {
                Advance();
            }

            var frame = new Frame(function, statements, _globals);
            frame.Arguments = prevFrame?.Arguments;
            _frameStack.Push(frame);
        }

        public void PushContinuation(IJumpTarget function, Statement statement, bool advance = true)
        {
            var block = statement as IBlock;
            var array = block?.Statements ?? ImmutableArray.Create(statement);
            PushContinuation(function, array, advance);
        }

        public void PushContinuation(IJumpTarget function, Statement statement, VariableTable arguments, bool advance = true)
        {
            PushContinuation(function, statement, advance);
            CurrentFrame.Arguments = arguments;
        }
    }
}
