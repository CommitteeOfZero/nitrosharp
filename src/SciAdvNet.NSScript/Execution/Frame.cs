using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    public interface IFrame
    {
        VariableTable Globals { get; }
        VariableTable NamedConstants { get; }
        VariableTable Arguments { get; }
    }

    internal sealed class Frame : IFrame
    {
        public Frame(ImmutableArray<Statement> statements, VariableTable globals)
        {
            Statements = statements;
            Globals = globals;
            Arguments = new VariableTable();
        }

        public ImmutableArray<Statement> Statements { get; }
        public int Position { get; set; }
        public VariableTable Globals { get; }
        public VariableTable NamedConstants { get; }
        public VariableTable Arguments { get; set; }
    }
}
