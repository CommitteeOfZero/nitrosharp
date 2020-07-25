namespace NitroSharp.NsScript.VM
{
    public readonly struct DialogueBlockToken
    {
        public readonly string BoxName;
        public readonly string BlockName;
        public readonly NsxModule Module;
        public readonly int SubroutineIndex;
        public readonly int Offset;

        public DialogueBlockToken(
            string boxName,
            string blockName,
            NsxModule module,
            int subroutineIndex,
            int offset)
        {
            BoxName = boxName;
            BlockName = blockName;
            Module = module;
            SubroutineIndex = subroutineIndex;
            Offset = offset;
        }
    }
}
