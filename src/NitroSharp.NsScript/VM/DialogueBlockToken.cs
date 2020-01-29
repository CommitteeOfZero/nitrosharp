namespace NitroSharp.NsScript.VM
{
    public readonly struct DialogueBlockToken
    {
        public readonly string BlockName;
        public readonly string BoxName;
        public readonly NsxModule Module;
        public readonly int SubroutineIndex;
        public readonly int Offset;

        public DialogueBlockToken(
            string blockName, string boxName, NsxModule module, int subroutineIndex, int offset)
        {
            BlockName = blockName;
            BoxName = boxName;
            Module = module;
            SubroutineIndex = subroutineIndex;
            Offset = offset;
        }
    }
}
