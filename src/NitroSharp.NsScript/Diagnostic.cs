using System;
using System.Globalization;
using System.IO;
using System.Text;

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

public enum SquiggleStyle
{
    Underline,
    VerticalBar
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

    public void Dump(TextWriter output, SquiggleStyle squiggleStyle)
    {
        SourceText sourceText = Location.SourceText;
        LinePositionSpan lineSpan = Location.GetLineSpan();

        (LinePosition startLine, LinePosition endLine) = lineSpan;

        string severity = Severity.ToString();
        string message = Message;

        output.WriteLine($"{severity}: {message}");
        output.WriteLine($"  --> {sourceText.FilePath.FileName}:{startLine.Line + 1}:{startLine.Column + 1}");
        output.WriteLine("     |");

        const string underlineSeqStart = "\e[4;31m";
        const string underlineSeqEnd = "\e[0m";

        if (startLine.Line > 0)
        {
            string before = sourceText.GetLineText(startLine.Line - 1);
            output.WriteLine($"{startLine.Line,4} | {before}");
        }

        var sb = new StringBuilder();
        for (int line = startLine.Line; line <= endLine.Line; line++)
        {
            string lineText = sourceText.GetLineText(line);

            (int squiggleStart, int squiggleEnd) = (0, lineText.Length);
            if (line == startLine.Line)
            {
                squiggleStart = startLine.Column;
                squiggleEnd = startLine.Line == endLine.Line ? endLine.Column : lineText.Length;
            }
            else if (line == endLine.Line)
            {
                (squiggleStart, squiggleEnd) = (0, endLine.Column);
            }

            bool zeroLength = (startLine == endLine) && (squiggleStart == squiggleEnd);
            if (zeroLength)
            {
                squiggleEnd = Math.Min(squiggleStart + 1, lineText.Length);
            }

            (string squiggleSeqStart, string squiggleSeqEnd) = squiggleStyle switch
            {
                SquiggleStyle.Underline => (underlineSeqStart, underlineSeqEnd),
                SquiggleStyle.VerticalBar => ("|", "|"),
                _ => ThrowHelper.Unreachable<(string, string)>()
            };

            sb.Append(lineText[..squiggleStart]);
            sb.Append(squiggleSeqStart);
            sb.Append(lineText[squiggleStart..squiggleEnd]);
            sb.Append(squiggleSeqEnd);
            sb.Append(lineText[squiggleEnd..]);

            output.WriteLine($"{line + 1,4} | {sb}");
            sb.Clear();
        }

        if (endLine.Line < sourceText.LineCount - 1)
        {
            string after = sourceText.GetLineText(endLine.Line + 1);
            output.WriteLine($"{endLine.Line + 2,4} | {after}");
        }

        output.WriteLine("     |");
    }
}
