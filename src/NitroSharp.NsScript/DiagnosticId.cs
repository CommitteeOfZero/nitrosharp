namespace NitroSharp.NsScript
{
    public enum DiagnosticId
    {
        UnterminatedString,
        UnterminatedQuotedIdentifier,
        UnterminatedComment,
        UnterminatedDialogueBlockStartTag,
        UnterminatedDialogueBlockIdentifier,
        NumberTooLarge,

        TokenExpected
    }
}
