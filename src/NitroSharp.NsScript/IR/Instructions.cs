using System;
using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.IR
{
    public static class Instructions
    {
        public static Instruction PushValue(ConstantValue value, SyntaxNode syntax) => new Instruction(Opcode.PushValue, value, syntax);
        public static Instruction PushGlobal(string name, Identifier syntax) => new Instruction(Opcode.PushGlobal, name, syntax);
        public static Instruction PushLocal(string name, Identifier syntax) => new Instruction(Opcode.PushLocal, name, syntax);
        public static Instruction ConvertToDelta(DeltaExpression syntax) => new Instruction(Opcode.ConvertToDelta, syntax);

        public static Instruction AssignParameter(string name, AssignmentOperatorKind operatorKind, AssignmentExpression synax)
            => new Instruction(Opcode.AssignLocal, name, operatorKind, synax);

        public static Instruction AssignGlobal(string name, AssignmentOperatorKind assignOperator, AssignmentExpression syntax)
            => new Instruction(Opcode.AssignGlobal, name, assignOperator, syntax);

        public static Instruction ApplyUnary(UnaryOperatorKind unaryOperator, UnaryExpression syntax)
            => new Instruction(Opcode.ApplyUnary, unaryOperator, syntax);

        public static Instruction ApplyBinary(BinaryOperatorKind operatorKind, BinaryExpression syntax)
            => new Instruction(Opcode.ApplyBinary, operatorKind, syntax);

        public static Instruction SetDialogueBlock(DialogueBlockSymbol dialogueBlock)
            => new Instruction(Opcode.SetDialogueBlock, dialogueBlock, null);

        public static Instruction Say(string text, PXmlString syntax) => new Instruction(Opcode.Say, text, syntax);
        public static Instruction WaitForInput(PXmlLineSeparator syntax) => new Instruction(Opcode.WaitForInput, syntax);

        public static Instruction Call(Symbol symbol, FunctionCall syntax) => new Instruction(Opcode.Call, symbol, syntax);
        public static Instruction CallChapter(string moduleName, CallChapterStatement syntax) => new Instruction(Opcode.CallFar, syntax);

        public static Instruction Nop() => new Instruction(Opcode.Nop, null);
        public static Instruction Jump(int targetInstructionIndex) => new Instruction(Opcode.Jump, targetInstructionIndex, null);
        public static Instruction JumpIfEquals(ConstantValue value, int targetInstructionIndex)
            => new Instruction(Opcode.JumpIfEquals, value, targetInstructionIndex, null);

        public static Instruction Return() => new Instruction(Opcode.Return, null);
    }
}
