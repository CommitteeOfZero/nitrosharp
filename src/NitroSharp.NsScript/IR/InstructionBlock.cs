namespace NitroSharp.NsScript.IR
{
    public sealed class InstructionBlock
    {
        private readonly Instruction[] _instructions;

        internal InstructionBlock(Instruction[] instructions)
        {
            _instructions = instructions;
        }

        public int Length => _instructions.Length;

        public ref Instruction FetchInstruction(int index)
        {
            return ref _instructions[index];
        }
    }
}
