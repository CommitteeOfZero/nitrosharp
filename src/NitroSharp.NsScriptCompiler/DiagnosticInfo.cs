using System;

namespace NitroSharp.NsScript
{
    public static class DiagnosticInfo
    {
        public static string GetMessage(DiagnosticId id)
        {
            switch (id)
            {
                case DiagnosticId.UnterminatedString:
                    return "String is not properly terminated.";
                case DiagnosticId.UnterminatedQuotedIdentifier:
                    return "Quoted identifier is not properly terminated.";
                case DiagnosticId.UnterminatedComment:
                    return "Comment is not properly terminated.";
                case DiagnosticId.UnterminatedDialogueBlockStartTag:
                    return "Dialogue block start tag is not properly terminated.";
                case DiagnosticId.UnterminatedDialogueBlockIdentifier:
                    return "Dialogue block identifier is not properly terminated.";

                case DiagnosticId.TokenExpected:
                    return "Expected '{0}', found '{1}'.";
                case DiagnosticId.MissingStatementTerminator:
                    return "Statement is not properly terminated.";
                case DiagnosticId.StrayToken:
                    return "Stray token '{0}'.";
                case DiagnosticId.MisplacedSemicolon:
                    return "Unexpected ';'.";
                case DiagnosticId.ExpectedSubroutineDeclaration:
                    return "Expected a subroutine declaration.";
                case DiagnosticId.InvalidExpressionStatement:
                    return "Only assignment, call, increment and decrement expressions can be used as a statement.";
                case DiagnosticId.InvalidExpressionTerm:
                    return "Invalid expression term '{0}'.";
                case DiagnosticId.StrayPXmlElement:
                    return "Stray PXml element.";

                case DiagnosticId.UnresolvedIdentifier:
                    return "Unresolved identifier '{0}'.";
                case DiagnosticId.BadAssignmentTarget:
                    return "The assignment target must be a variable.";
                case DiagnosticId.ExternalModuleNotFound:
                    return "External module '{0}' is not found.";
                case DiagnosticId.ChapterMainNotFound:
                    return "The target module of a call_chapter expression does not have chapter 'main'.";
                case DiagnosticId.InvalidBezierCurve:
                    return "The specified bezier curve does not meet the requirements of the engine.";

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(id));
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
                case DiagnosticId.ExpectedSubroutineDeclaration:
                case DiagnosticId.UnresolvedIdentifier:
                case DiagnosticId.BadAssignmentTarget:
                case DiagnosticId.ExternalModuleNotFound:
                case DiagnosticId.ChapterMainNotFound:
                    return DiagnosticSeverity.Error;

                case DiagnosticId.MisplacedSemicolon:
                case DiagnosticId.StrayPXmlElement:
                case DiagnosticId.StrayToken:
                    return DiagnosticSeverity.Warning;

                case DiagnosticId.MissingStatementTerminator:
                    return DiagnosticSeverity.Info;

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(diagnosticId));
            }
        }
    }
}
