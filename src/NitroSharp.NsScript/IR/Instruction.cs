using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.IR
{
    public struct Instruction
    {
        internal Instruction(Opcode opcode, SyntaxNode syntax)
        {
            Opcode = opcode;
            Operand1 = Operand2 = null;
            Syntax = syntax;
        }

        internal Instruction(Opcode opcode, object operand, SyntaxNode syntax)
        {
            Opcode = opcode;
            Operand1 = operand;
            Operand2 = null;
            Syntax = syntax;
        }

        internal Instruction(Opcode opcode, object operand1, object operand2, SyntaxNode syntax)
        {
            Opcode = opcode;
            Operand1 = operand1;
            Operand2 = operand2;
            Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }
        public Opcode Opcode { get; }
        public object Operand1 { get; }
        public object Operand2 { get; }

        public override string ToString()
        {
            return $"<{Opcode} {string.Join(",", Operand1, Operand2)}>";
        }
    }
}
