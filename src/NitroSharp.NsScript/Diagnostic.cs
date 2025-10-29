using System.Globalization;

namespace NitroSharp.NsScript;

public enum DiagnosticId
{
    UnterminatedString,
    UnterminatedQuotedIdentifier,
    UnterminatedComment,
    UnterminatedDialogueBlockStartTag,
    UnterminatedDialogueBlockIdentifier,
    NumberTooLarge,

    TokenExpected,
    StrayToken,
    MisplacedSemicolon,
    ExpectedSubroutineDeclaration,
    MissingStatementTerminator,
    InvalidExpressionTerm,
    InvalidExpressionStatement,
    StrayMarkupBlock,
    MisplacedBreak,
    OrphanedSelectSection,
    InvalidBezierCurve,

    UnresolvedIdentifier,
    BadAssignmentTarget,
    ExternalModuleNotFound,
    ChapterMainNotFound
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public class Diagnostic
{
    public static Diagnostic Create(SourceLocation location, DiagnosticId id)
        => new(location, id);

    public static Diagnostic Create(SourceLocation location, DiagnosticId id, params object[] arguments)
        => new DiagnosticWithArguments(location, id, arguments);

    private Diagnostic(SourceLocation location, DiagnosticId id)
    {
        Location = location;
        Id = id;
    }

    public DiagnosticId Id { get; }
    public SourceLocation Location { get; }
    public TextSpan Span => Location.Span; // Convenience property
    public virtual string Message => DiagnosticInfo.GetMessage(Id);
    public DiagnosticSeverity Severity => DiagnosticInfo.GetSeverity(Id);

    private sealed class DiagnosticWithArguments(SourceLocation location, DiagnosticId id, params object[] arguments)
        : Diagnostic(location, id)
    {
        private string? _message;

        public override string Message => _message ??= FormatMessage();

        private string FormatMessage()
        {
            string formatString = DiagnosticInfo.GetMessage(Id);
            return string.Format(CultureInfo.CurrentCulture, formatString, arguments);
        }
    }
}
