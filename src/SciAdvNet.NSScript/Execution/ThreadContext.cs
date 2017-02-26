using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    internal sealed class ThreadContext
    {
        private VariableTable _globals;
        private readonly Stack<Frame> _frameStack;

        public ThreadContext(Module module, Statement target, VariableTable globals)
        {
            CurrentModule = module;
            _globals = globals;
            _frameStack = new Stack<Frame>();
            PushContinuation(target);
        }

        public Module CurrentModule { get; }
        public Frame CurrentFrame => _frameStack.Peek();
        public SyntaxNode CurrentNode => CurrentFrame.Statements[CurrentFrame.Position];
        public bool DoneExecuting => _frameStack.Count == 0;

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

        public void PushContinuation(ImmutableArray<Statement> statements)
        {
            Advance();
            var frame = new Frame(statements, _globals);
            _frameStack.Push(frame);
        }

        public void PushContinuation(Statement statement)
        {
            var block = statement as IBlock;
            var array = block?.Statements ?? ImmutableArray.Create(statement);
            PushContinuation(array);
        }

        public void PushContinuation(Statement statement, VariableTable arguments)
        {
            PushContinuation(statement);
            CurrentFrame.Arguments = arguments;
        }
    }
}
