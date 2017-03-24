using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    public interface IFrame
    {
        VariableTable Globals { get; }
        VariableTable Arguments { get; }
    }

    internal sealed class Frame : IFrame
    {
        public Frame(ImmutableArray<Statement> statements, VariableTable globals)
        {
            Statements = statements;
            Globals = globals;
            Arguments = new VariableTable();

            OperandStack = new Queue<Expression>();
            OperationStack = new Queue<OperationKind>();
        }

        public ImmutableArray<Statement> Statements { get; }
        public int Position { get; set; }
        public VariableTable Globals { get; }
        public VariableTable Arguments { get; set; }

        public Queue<Expression> OperandStack { get; }
        public Queue<OperationKind> OperationStack { get; }
    }
}
