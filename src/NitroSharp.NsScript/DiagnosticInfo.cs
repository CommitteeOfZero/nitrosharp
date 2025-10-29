namespace NitroSharp.NsScript;

internal static class DiagnosticInfo
{
    public static string GetMessage(DiagnosticId id)
    {
        return id switch
        {
            DiagnosticId.UnterminatedString => "String is not properly terminated.",
            DiagnosticId.UnterminatedQuotedIdentifier => "Quoted identifier is not properly terminated.",
            DiagnosticId.UnterminatedComment => "Comment is not properly terminated.",
            DiagnosticId.UnterminatedDialogueBlockStartTag => "Dialogue block start tag is not properly terminated.",
            DiagnosticId.UnterminatedDialogueBlockIdentifier => "Dialogue block identifier is not properly terminated.",
            DiagnosticId.TokenExpected => "Expected '{0}', found '{1}'.",
            DiagnosticId.MissingStatementTerminator => "Statement is not properly terminated.",
            DiagnosticId.StrayToken => "Stray token '{0}'.",
            DiagnosticId.MisplacedSemicolon => "Unexpected ';'.",
            DiagnosticId.ExpectedSubroutineDeclaration => "Expected a subroutine declaration.",
            DiagnosticId.InvalidExpressionStatement => "Only assignment, call, increment and decrement expressions can be used as a statement.",
            DiagnosticId.InvalidExpressionTerm => "Invalid expression term '{0}'.",
            DiagnosticId.StrayMarkupBlock => "Stray markup block.",
            DiagnosticId.MisplacedBreak => "Break statement cannot be used outside of a looping construct.",
            DiagnosticId.OrphanedSelectSection => "Select section cannot appear outside of a select statement.",
            DiagnosticId.InvalidBezierCurve => "The specified bezier curve does not meet the requirements of the engine.",
            DiagnosticId.UnresolvedIdentifier => "Unresolved identifier '{0}'.",
            DiagnosticId.BadAssignmentTarget => "The assignment target must be a variable.",
            DiagnosticId.ExternalModuleNotFound => "External module '{0}' is not found.",
            DiagnosticId.ChapterMainNotFound => "The target module of a call_chapter expression does not have chapter 'main'.",
            _ => throw ThrowHelper.UnexpectedValue(nameof(id))
        };
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
            case DiagnosticId.ExpectedSubroutineDeclaration:
            case DiagnosticId.UnresolvedIdentifier:
            case DiagnosticId.BadAssignmentTarget:
            case DiagnosticId.ExternalModuleNotFound:
            case DiagnosticId.ChapterMainNotFound:
                return DiagnosticSeverity.Error;

            case DiagnosticId.MisplacedSemicolon:
            case DiagnosticId.StrayMarkupBlock:
            case DiagnosticId.StrayToken:
            case DiagnosticId.MisplacedBreak:
            case DiagnosticId.OrphanedSelectSection:
            case DiagnosticId.InvalidBezierCurve:
                return DiagnosticSeverity.Warning;

            case DiagnosticId.MissingStatementTerminator:
                return DiagnosticSeverity.Info;

            default:
                throw ThrowHelper.UnexpectedValue(nameof(diagnosticId));
        }
    }
}
