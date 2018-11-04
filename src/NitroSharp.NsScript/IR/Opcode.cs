namespace NitroSharp.NsScript.IR
{
    public enum Opcode : byte
    {
        PushGlobal,
        PushLocal,
        PushValue,

        ConvertToDelta,
        ApplyBinary,
        ApplyUnary,
        AssignGlobal,
        AssignLocal,

        Call,
        CallFar,

        SetDialogueBlock,
        Say,
        WaitForInput,

        JumpIfEquals,
        Nop,
        Jump,
        Return,
        GetSelectedChoice,
        JumpIfNotEquals,
        Select
    }
}
