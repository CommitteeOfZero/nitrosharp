using NitroSharp.NsScript.IR;
using NitroSharp.NsScript.Symbols;
using System;

namespace NitroSharp.NsScript.Execution
{
    internal sealed class Frame
    {
        private readonly InstructionBlock _instructions;
        private int _pc;

        public Frame(MergedSourceFileSymbol module, InvocableSymbol symbol)
        {
            Module = module;
            Symbol = symbol;
            Arguments = Environment.Empty;
            _instructions = symbol.LinearRepresentation;
        }

        public MergedSourceFileSymbol Module { get; }
        public InvocableSymbol Symbol { get; }
        public Environment Arguments { get; private set; }

        public ref Instruction FetchInstruction() => ref _instructions.FetchInstruction(_pc);

        public void Jump(int targetInstructionIndex)
        {
            if (targetInstructionIndex < 0 || targetInstructionIndex >= _instructions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(targetInstructionIndex));
            }

            _pc = targetInstructionIndex;
        }

        public bool Advance()
        {
            if (_pc >= _instructions.Length)
            {
                return false;
            }

            _pc++;
            return true;
        }

        public void SetArgument(string name, ConstantValue value)
        {
            if (ReferenceEquals(Arguments, Environment.Empty))
            {
                Arguments = new Environment();
            }

            Arguments.Set(name, value);
        }
    }
}
