using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    public sealed class Frame
    {
        public Frame(ImmutableArray<Statement> statements, VariableTable globals)
        {
            Statements = statements;
            Globals = globals;
            Arguments = new VariableTable();

            OperandStack = new Stack<Expression>();
            OperationStack = new Stack<OperationKind>();
            EvaluationStack = new Stack<Expression>();
        }

        public ImmutableArray<Statement> Statements { get; }
        public int Position { get; set; }
        public VariableTable Globals { get; }
        public VariableTable Arguments { get; set; }

        public Stack<Expression> OperandStack { get; }
        public Stack<OperationKind> OperationStack { get; }
        public Stack<Expression> EvaluationStack { get; }
    }
}
