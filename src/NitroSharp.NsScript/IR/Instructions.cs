using System;
using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.IR
{
    public static class Instructions
    {
        public static Instruction PushValue(ConstantValue value) => new Instruction(Opcode.PushValue, value);
        public static Instruction PushGlobal(string name) => new Instruction(Opcode.PushGlobal, name);
        public static Instruction PushLocal(string name) => new Instruction(Opcode.PushLocal, name);
        public static Instruction ConvertToDelta() => new Instruction(Opcode.ConvertToDelta);

        public static Instruction AssignParameter(string name, AssignmentOperatorKind operatorKind)
            => new Instruction(Opcode.AssignLocal, name, operatorKind);

        public static Instruction AssignGlobal(string name, AssignmentOperatorKind assignOperator)
            => new Instruction(Opcode.AssignGlobal, name, assignOperator);

        public static Instruction ApplyUnary(UnaryOperatorKind unaryOperator)
            => new Instruction(Opcode.ApplyUnary, unaryOperator);

        public static Instruction ApplyBinary(BinaryOperatorKind operatorKind)
            => new Instruction(Opcode.ApplyBinary, operatorKind);

        public static Instruction SetDialogueBlock(DialogueBlockSymbol dialogueBlock)
            => new Instruction(Opcode.SetDialogueBlock, dialogueBlock);

        public static Instruction Say(string text, PXmlString syntax) => new Instruction(Opcode.Say, text);
        public static Instruction WaitForInput(PXmlLineSeparator syntax) => new Instruction(Opcode.WaitForInput);

        public static Instruction Call(Symbol symbol) => new Instruction(Opcode.Call, symbol);
        public static Instruction CallFar(string modulePath, string symbolName)
            => new Instruction(Opcode.CallFar, modulePath, symbolName);

        public static Instruction Nop() => new Instruction(Opcode.Nop);
        public static Instruction Jump(int targetInstructionIndex) => new Instruction(Opcode.Jump, targetInstructionIndex);
        public static Instruction JumpIfEquals(ConstantValue value, int targetInstructionIndex)
            => new Instruction(Opcode.JumpIfEquals, value, targetInstructionIndex);
        public static Instruction JumpIfNotEquals(ConstantValue value, int targetInstructionIndex)
            => new Instruction(Opcode.JumpIfNotEquals, value, targetInstructionIndex);

        public static Instruction Return() => new Instruction(Opcode.Return);

        public static Instruction Select() => new Instruction(Opcode.Select);
        public static Instruction GetSelectedChoice() => new Instruction(Opcode.GetSelectedChoice);
    }
}
