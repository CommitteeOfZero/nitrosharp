namespace NitroSharp.NsScript
{
    public static class DiagnosticInfo
    {
        public static string GetMessage(DiagnosticId id)
        {
            switch (id)
            {
                case DiagnosticId.UnterminatedString:
                    return Resources.UnterminatedString;
                case DiagnosticId.UnterminatedQuotedIdentifier:
                    return Resources.UnterminatedQuotedIdentifier;
                case DiagnosticId.UnterminatedComment:
                    return Resources.UnterminatedComment;
                case DiagnosticId.UnterminatedDialogueBlockStartTag:
                    return Resources.UnterminatedDialogueBlockStartTag;
                case DiagnosticId.UnterminatedDialogueBlockIdentifier:
                    return Resources.UnterminatedDialogueBlockIdentifier;

                default:
                    return string.Empty;
            }
        }

        public static DiagnosticSeverity GetSeverity(DiagnosticId diagnosticId)
        {
            switch (diagnosticId)
            {
                case DiagnosticId.UnterminatedString:
                case DiagnosticId.UnterminatedQuotedIdentifier:
                case DiagnosticId.UnterminatedComment:
                case DiagnosticId.UnterminatedDialogueBlockStartTag:
                case DiagnosticId.UnterminatedDialogueBlockIdentifier:
                    return DiagnosticSeverity.Error;

                default:
                    return DiagnosticSeverity.Warning;
            }
        }
    }
}
