using System;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScript.Syntax;

public enum SyntaxTokenKind : byte
{
    None = 0,
    BadToken,

    IncludeDirective,

    Identifier,
    NumericLiteral,
    StringLiteralOrQuotedIdentifier,

    // Punctuation
    OpenBrace,
    CloseBrace,
    OpenParen,
    CloseParen,
    Comma,
    Dot,
    Colon,
    Semicolon,
    Equals,
    Plus,
    Minus,
    Asterisk,
    Slash,
    LessThan,
    GreaterThan,
    Exclamation,
    Ampersand,
    At,
    Dollar,
    Hash,
    Percent,
    Arrow,
    AtArrow,

    // Compound punctuation
    EqualsEquals,
    PlusPlus,
    MinusMinus,
    PlusEquals,
    MinusEquals,
    AsteriskEquals,
    SlashEquals,
    LessThanEquals,
    GreaterThanEquals,
    ExclamationEquals,
    BarBar,
    AmpersandAmpersand,

    // Keywords
    ChapterKeyword,
    FunctionKeyword,
    SceneKeyword,
    CallSceneKeyword,
    CallChapterKeyword,
    NullKeyword,
    TrueKeyword,
    FalseKeyword,
    WhileKeyword,
    IfKeyword,
    ElseKeyword,
    SelectKeyword,
    CaseKeyword,
    BreakKeyword,
    ReturnKeyword,

    DialogueBlockStartTag,
    DialogueBlockEndTag,
    DialogueBlockIdentifier,
    Markup,
    MarkupBlankLine,
    EndOfFile
}

public enum SigilKind
{
    None,
    Dollar,
    Hash
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct SyntaxToken
{
    internal SyntaxToken(SyntaxTokenKind kind, TextSpan textSpan, SyntaxTokenFlags flags)
    {
        TextSpan = textSpan;
        Kind = kind;
        Flags = flags;
    }

    public readonly TextSpan TextSpan;
    public readonly SyntaxTokenKind Kind;
    public readonly SyntaxTokenFlags Flags;

    public bool IsFloatingPointLiteral =>
        (Flags & SyntaxTokenFlags.HasDecimalPoint) == SyntaxTokenFlags.HasDecimalPoint;

    public bool IsHexTriplet =>
        (Flags & SyntaxTokenFlags.IsHexTriplet) == SyntaxTokenFlags.IsHexTriplet;

    public bool HasDiagnostics
        => (Flags & SyntaxTokenFlags.HasDiagnostics) == SyntaxTokenFlags.HasDiagnostics;

    public bool HasSigil =>
        (Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix ||
        (Flags & SyntaxTokenFlags.HasHashPrefix) == SyntaxTokenFlags.HasHashPrefix;

    public static LexingResult Lex(string text, LexingMode lexingMode = LexingMode.Normal)
        => Lex(SourceText.From(text), lexingMode);

    public static LexingResult Lex(SourceText sourceText, LexingMode lexingMode = LexingMode.Normal)
    {
        var diagnostics = new DiagnosticBuilder();
        var lexer = new Lexer(sourceText, diagnostics, lexingMode);
        return new LexingResult(lexer);
    }

    public SigilKind GetSigil()
    {
        if ((Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix)
        {
            return SigilKind.Dollar;
        }
        if ((Flags & SyntaxTokenFlags.HasHashPrefix) == SyntaxTokenFlags.HasHashPrefix)
        {
            return SigilKind.Hash;
        }

        return SigilKind.None;
    }

    public TextSpan GetValueSpan()
    {
        int start = TextSpan.Start;
        int end = TextSpan.End;
        if ((Flags & SyntaxTokenFlags.IsQuoted) == SyntaxTokenFlags.IsQuoted)
        {
            start++;
            end--;
        }
        if ((Flags & SyntaxTokenFlags.HasDollarPrefix) == SyntaxTokenFlags.HasDollarPrefix
            || (Flags & SyntaxTokenFlags.HasHashPrefix) == SyntaxTokenFlags.HasHashPrefix
            || (Flags & SyntaxTokenFlags.IsHexTriplet) == SyntaxTokenFlags.IsHexTriplet)
        {
            start++;
        }

        return TextSpan.FromBounds(start, end);
    }

    public override string ToString() => $"{TextSpan} <{Kind}>";
}

[Flags]
public enum SyntaxTokenFlags : byte
{
    Empty = 0,
    HasDiagnostics = 1 << 0,
    IsQuoted = 1 << 1,
    HasDollarPrefix = 1 << 2,
    HasHashPrefix = 1 << 3,
    HasDecimalPoint = 1 << 5,
    IsHexTriplet = 1 << 6
}
