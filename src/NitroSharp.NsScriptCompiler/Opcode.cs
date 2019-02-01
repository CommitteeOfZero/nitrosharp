namespace NitroSharp.NsScriptNew
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
        LoadArg0 = 0x17,        // <>
        LoadArg1 = 0x18,        // <>
        LoadArg2 = 0x19,        // <>
        LoadArg3 = 0x1A,        // <>
        LoadArg = 0x1B,         // <ushort slot>
        LoadVar = 0x1C,         // <ushort slot>
        StoreArg0 = 0x1D,       // <>
        StoreArg1 = 0x1E,       // <>
        StoreArg2 = 0x1F,       // <>
        StoreArg3 = 0x20,       // <>
        StoreArg = 0x21,        // <ushort slot>
        StoreVar = 0x22,        // <ushort slot>
        Inc = 0x23,             // <>
        Dec = 0x24,             // <>
        Neg = 0x25,             // <>
        Equal = 0x26,           // <>
        NotEqual = 0x27,        // <>
        Binary = 0x28,          // <byte operator>

        Jump = 0x30,            // <short offset>
        JumpIfTrue = 0x31,      // <short offset>
        JumpIfFalse = 0x32,     // <short offset>
        Dispatch = 0x33,        // <byte function>
        Call = 0x34,            // <ushort subroutine>
        CallFar = 0x35,         // <ushort module> <ushort subroutine>
        Return = 0x36,          // <>
        Select = 0x37,          // <>,

        PresentText = 0x40,     // <ushort token>,
        AwaitInput = 0x41,      // <>
    }
}
