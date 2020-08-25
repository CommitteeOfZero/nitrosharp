namespace NitroSharp.NsScript
{
    public enum Opcode : byte
    {
        Nop = 0x00,

        LoadImm0 = 0x10,        // <>
        LoadImm1 = 0x11,        // <>
        LoadImmTrue = 0x12,     // <>
        LoadImmFalse = 0x13,    // <>
        LoadImmNull = 0x14,     // <>
        LoadImmEmptyStr = 0x15, // <>
        LoadImm = 0x16,         // <byte type> <value>
        LoadVar = 0x17,         // <ushort slot>
        StoreVar = 0x18,        // <ushort slot>

        Inc = 0x19,             // <>
        Dec = 0x1A,             // <>
        Neg = 0x1B,             // <>
        Invert = 0x1C,          // <>
        Delta = 0x1D,           // <>

        Equal = 0x20,           // <>
        NotEqual = 0x21,        // <>
        Binary = 0x22,          // <byte operator>

        Jump = 0x30,            // <short offset>
        JumpIfTrue = 0x31,      // <short offset>
        JumpIfFalse = 0x32,     // <short offset>
        Dispatch = 0x33,        // <byte function> <byte argCount>
        Call = 0x34,            // <ushort subroutine> <byte argCount>
        CallFar = 0x35,         // <ushort module> <ushort subroutine> <byte argCount>
        CallScene = 0x36,       // <ushort module> <ushort scene>
        Return = 0x37,          // <>

        SelectStart = 0x40,     // <>
        IsPressed = 0x41,       // <ushort token>
        SelectEnd = 0x42,       // <>
        BezierStart = 0x43,     // <>
        BezierEndSeg = 0x44,    // <>
        BezierEnd = 0x45,       // <>

        ActivateBlock = 0x50,   // <ushort id>
        ClearPage = 0x51,
        AppendDialogue = 0x52,  // <ushort token>
        LineEnd = 0x53,         // <>
    }
}
