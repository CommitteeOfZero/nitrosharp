using System.Collections.Generic;
using System.Collections.Immutable;

namespace CommitteeOfZero.NsScript.Execution
{
    public sealed class Frame
    {
        public Frame(IJumpTarget function, ImmutableArray<Statement> statements, VariableTable globals)
        {
            Function = function;
            Statements = statements;
            Globals = globals;
            Arguments = new VariableTable();

            OperandStack = new Stack<Expression>();
            OperationStack = new Stack<OperationKind>();
            EvaluationStack = new Stack<Expression>();
        }

        public IJumpTarget Function { get; }
        public ImmutableArray<Statement> Statements { get; }
        public int Position { get; set; }
        public VariableTable Globals { get; }
        public VariableTable Arguments { get; set; }

        public Expression CurrentExpression { get; internal set; }
        public Stack<Expression> OperandStack { get; }
        public Stack<OperationKind> OperationStack { get; }
        public Stack<Expression> EvaluationStack { get; }
    }
}
