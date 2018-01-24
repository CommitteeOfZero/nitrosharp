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
        CallChapter,

        SetDialogueBlock,
        Say,
        WaitForInput,

        JumpIfEquals,
        Nop,
        Jump,
        Return
    }
}
