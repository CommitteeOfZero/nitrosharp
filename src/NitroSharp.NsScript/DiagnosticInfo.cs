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

                case DiagnosticId.TokenExpected:
                    return Resources.TokenExpected;
                case DiagnosticId.MissingStatementTerminator:
                    return Resources.MissingStatementTerminator;
                case DiagnosticId.StrayToken:
                    return Resources.StrayToken;
                case DiagnosticId.MisplacedSemicolon:
                    return Resources.MisplacedSemicolon;
                case DiagnosticId.ExpectedMemberDeclaration:
                    return Resources.ExpectedMemeberDeclaration;
                case DiagnosticId.InvalidExpressionStatement:
                    return Resources.InvalidExpressionStatement;
                case DiagnosticId.InvalidExpressionTerm:
                    return Resources.InvalidExpressionTerm;
                case DiagnosticId.StrayPXmlElement:
                    return Resources.StrayPXmlElement;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(id));
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
                case DiagnosticId.TokenExpected:
                case DiagnosticId.InvalidExpressionStatement:
                case DiagnosticId.InvalidExpressionTerm:
                case DiagnosticId.ExpectedMemberDeclaration:
                    return DiagnosticSeverity.Error;

                case DiagnosticId.MisplacedSemicolon:
                case DiagnosticId.StrayPXmlElement:
                case DiagnosticId.StrayToken:
                    return DiagnosticSeverity.Warning;

                case DiagnosticId.MissingStatementTerminator:
                    return DiagnosticSeverity.Info;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(diagnosticId));
            }
        }
    }
}
