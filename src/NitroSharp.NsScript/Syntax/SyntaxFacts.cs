using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Syntax;

public static class SyntaxFacts
{
    private const char EofCharacter = char.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDecDigit(char c) => c is >= '0' and <= '9';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' ||
            c is >= 'A' and <= 'F' ||
            c is >= 'a' and <= 'f';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhitespace(char c)
    {
        switch (c)
        {
            case ' ':
            case '\r':
            case '\n':
            case '\t':
            case (char)12288:
                return true;

            default:
                Debug.Assert(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.SpaceSeparator);
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNewLine(char c) => c is '\r' or '\n';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSigil(char c) => c is '$' or '#' or '@';

    public static bool TryGetKeywordKind(ReadOnlySpan<char> text, out SyntaxTokenKind kind)
        => KeywordScanner.TryRecognizeKeyword(text, out kind);

    public static SyntaxTokenKind GetKeywordKind(ReadOnlySpan<char> text)
        => KeywordScanner.RecognizeKeyword(text);

    public static bool IsIdentifierStartCharacter(char c, char next)
        => IsIdentifierPartCharacter(c, next) && !IsDecDigit(c);

    public static bool IsIdentifierStopCharacter(char c, char next)
        => !IsIdentifierPartCharacter(c, next);

    public static bool IsIdentifierPartCharacter(char c, char next)
    {
        switch (c)
        {
            case ' ':
            case '"':
            case '\t':
            case '\r':
            case '\n':
            case ',':
            case ':':
            case ';':
            case '{':
            case '}':
            case '(':
            case ')':
            case '=':
            case '+':
            case '*':
            case '/':
            case '<':
            case '>':
            case '%':
            case '!':
            case '|':
            case '&':
            case '.':
            case '$':
            case '#':
            case '@':
            case EofCharacter:
                return false;
            // Hack: O-FRONT is a valid identifier, but O-42 is not.
            case '-':
                return char.IsLetter(next);
            default:
                return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStatementTerminator(SyntaxTokenKind tokenKind)
    {
        return tokenKind is SyntaxTokenKind.Semicolon or SyntaxTokenKind.Colon;
    }

    public static bool CanStartDeclaration(SyntaxTokenKind tokenKind)
    {
        switch (tokenKind)
        {
            case SyntaxTokenKind.ChapterKeyword:
            case SyntaxTokenKind.SceneKeyword:
            case SyntaxTokenKind.FunctionKeyword:
                return true;

            default:
                return false;
        }
    }

    public static bool IsDefiniteStatementStart(SyntaxTokenKind tokenKind)
    {
        switch (tokenKind)
        {
            case SyntaxTokenKind.OpenBrace:
            case SyntaxTokenKind.IfKeyword:
            case SyntaxTokenKind.BreakKeyword:
            case SyntaxTokenKind.WhileKeyword:
            case SyntaxTokenKind.ReturnKeyword:
            case SyntaxTokenKind.SelectKeyword:
            case SyntaxTokenKind.CaseKeyword:
            case SyntaxTokenKind.CallChapterKeyword:
            case SyntaxTokenKind.CallSceneKeyword:
            case SyntaxTokenKind.DialogueBlockStartTag:
                return true;

            default:
                return false;
        }
    }

    public static bool CanStartStatement(SyntaxTokenKind tokenKind)
    {
        return IsDefiniteStatementStart(tokenKind) || CanStartExpressionTerm(tokenKind);
    }

    public static bool CanStartExpressionTerm(SyntaxTokenKind tokenKind)
    {
        switch (tokenKind)
        {
            case SyntaxTokenKind.Identifier:
            case SyntaxTokenKind.StringLiteralOrQuotedIdentifier:
            case SyntaxTokenKind.NumericLiteral:
            case SyntaxTokenKind.NullKeyword:
            case SyntaxTokenKind.TrueKeyword:
            case SyntaxTokenKind.FalseKeyword:
            case SyntaxTokenKind.OpenParen:
                return true;

            default:
                return false;
        }
    }

    public static bool IsStatementExpression(Expression expression)
    {
        SyntaxNodeKind kind = expression.Kind;
        return kind is SyntaxNodeKind.AssignmentExpression or SyntaxNodeKind.FunctionCallExpression;
    }

    public static UnaryOperatorKind? TryGetUnaryOperatorKind(SyntaxTokenKind operatorTokenKind)
    {
        return operatorTokenKind switch
        {
            SyntaxTokenKind.Exclamation => UnaryOperatorKind.Not,
            SyntaxTokenKind.Plus => UnaryOperatorKind.Plus,
            SyntaxTokenKind.Minus => UnaryOperatorKind.Minus,
            SyntaxTokenKind.At => UnaryOperatorKind.Delta,
            _ => null
        };
    }

    public static BinaryOperatorKind? TryGetBinaryOperatorKind(SyntaxTokenKind operatorTokenKind)
    {
        return operatorTokenKind switch
        {
            SyntaxTokenKind.Plus => BinaryOperatorKind.Add,
            SyntaxTokenKind.Minus => BinaryOperatorKind.Subtract,
            SyntaxTokenKind.Asterisk => BinaryOperatorKind.Multiply,
            SyntaxTokenKind.Slash => BinaryOperatorKind.Divide,
            SyntaxTokenKind.Percent => BinaryOperatorKind.Remainder,
            SyntaxTokenKind.LessThan => BinaryOperatorKind.LessThan,
            SyntaxTokenKind.LessThanEquals => BinaryOperatorKind.LessThanOrEqual,
            SyntaxTokenKind.GreaterThan => BinaryOperatorKind.GreaterThan,
            SyntaxTokenKind.GreaterThanEquals => BinaryOperatorKind.GreaterThanOrEqual,
            SyntaxTokenKind.BarBar => BinaryOperatorKind.Or,
            SyntaxTokenKind.AmpersandAmpersand => BinaryOperatorKind.And,
            SyntaxTokenKind.EqualsEquals => BinaryOperatorKind.Equals,
            SyntaxTokenKind.ExclamationEquals => BinaryOperatorKind.NotEquals,
            _ => null
        };
    }

    public static AssignmentOperatorKind? TryGetAssignmentOperatorKind(SyntaxTokenKind operatorTokenKind)
    {
        return operatorTokenKind switch
        {
            SyntaxTokenKind.Equals => AssignmentOperatorKind.Assign,
            SyntaxTokenKind.PlusEquals => AssignmentOperatorKind.AddAssign,
            SyntaxTokenKind.MinusEquals => AssignmentOperatorKind.SubtractAssign,
            SyntaxTokenKind.AsteriskEquals => AssignmentOperatorKind.MultiplyAssign,
            SyntaxTokenKind.SlashEquals => AssignmentOperatorKind.DivideAssign,
            SyntaxTokenKind.PlusPlus => AssignmentOperatorKind.Increment,
            SyntaxTokenKind.MinusMinus => AssignmentOperatorKind.Decrement,
            _ => null
        };
    }

    public static string GetText(SyntaxTokenKind kind)
    {
        return kind switch
        {
            SyntaxTokenKind.Dollar => "$",
            SyntaxTokenKind.Hash => "#",
            SyntaxTokenKind.At => "@",
            SyntaxTokenKind.Exclamation => "!",
            SyntaxTokenKind.Ampersand => "&",
            SyntaxTokenKind.Asterisk => "*",
            SyntaxTokenKind.OpenParen => "(",
            SyntaxTokenKind.CloseParen => ")",
            SyntaxTokenKind.Minus => "-",
            SyntaxTokenKind.Plus => "+",
            SyntaxTokenKind.Equals => "=",
            SyntaxTokenKind.OpenBrace => "{",
            SyntaxTokenKind.CloseBrace => "}",
            SyntaxTokenKind.Colon => ":",
            SyntaxTokenKind.Semicolon => ";",
            SyntaxTokenKind.LessThan => "<",
            SyntaxTokenKind.Comma => ",",
            SyntaxTokenKind.GreaterThan => ">",
            SyntaxTokenKind.Dot => ".",
            SyntaxTokenKind.Slash => "/",
            SyntaxTokenKind.Percent => "%",
            SyntaxTokenKind.Arrow => "->",
            SyntaxTokenKind.AtArrow => "@->",

            // compound
            SyntaxTokenKind.BarBar => "||",
            SyntaxTokenKind.AmpersandAmpersand => "&&",
            SyntaxTokenKind.MinusMinus => "--",
            SyntaxTokenKind.PlusPlus => "++",
            SyntaxTokenKind.ExclamationEquals => "!=",
            SyntaxTokenKind.EqualsEquals => "==",
            SyntaxTokenKind.LessThanEquals => "<=",
            SyntaxTokenKind.GreaterThanEquals => ">=",
            SyntaxTokenKind.SlashEquals => "/=",
            SyntaxTokenKind.AsteriskEquals => "*=",
            SyntaxTokenKind.PlusEquals => "+=",
            SyntaxTokenKind.MinusEquals => "-=",

            SyntaxTokenKind.ChapterKeyword => "chapter",
            SyntaxTokenKind.FunctionKeyword => "function",
            SyntaxTokenKind.SceneKeyword => "scene",
            SyntaxTokenKind.CallSceneKeyword => "call_scene",
            SyntaxTokenKind.CallChapterKeyword => "call_chapter",
            SyntaxTokenKind.NullKeyword => "null",
            SyntaxTokenKind.TrueKeyword => "true",
            SyntaxTokenKind.FalseKeyword => "false",
            SyntaxTokenKind.WhileKeyword => "while",
            SyntaxTokenKind.IfKeyword => "if",
            SyntaxTokenKind.ElseKeyword => "else",
            SyntaxTokenKind.SelectKeyword => "select",
            SyntaxTokenKind.CaseKeyword => "case",
            SyntaxTokenKind.BreakKeyword => "break",
            SyntaxTokenKind.ReturnKeyword => "return",

            SyntaxTokenKind.IncludeDirective => "#include",
            SyntaxTokenKind.MarkupBlankLine => "\r\n",
            SyntaxTokenKind.DialogueBlockEndTag => "</PRE>",
            SyntaxTokenKind.EndOfFile => "<EOF>",
            _ => string.Empty,
        };
    }

    private static class KeywordScanner
    {
        public static bool TryRecognizeKeyword(ReadOnlySpan<char> text, out SyntaxTokenKind keywordKind)
            => (keywordKind = RecognizeKeyword(text)) != SyntaxTokenKind.EndOfFile;

        public static SyntaxTokenKind RecognizeKeyword(ReadOnlySpan<char> text)
        {
            return text switch
            {
                "chapter" => SyntaxTokenKind.ChapterKeyword,
                "function" => SyntaxTokenKind.FunctionKeyword,
                "scene" => SyntaxTokenKind.SceneKeyword,
                "call_scene" => SyntaxTokenKind.CallSceneKeyword,
                "call_chapter" => SyntaxTokenKind.CallChapterKeyword,
                "null" or "Null" or "NULL" => SyntaxTokenKind.NullKeyword,
                "true" or "True" or "TRUE" => SyntaxTokenKind.TrueKeyword,
                "false" or "False" or "FALSE" => SyntaxTokenKind.FalseKeyword,
                "while" => SyntaxTokenKind.WhileKeyword,
                "if" => SyntaxTokenKind.IfKeyword,
                "else" => SyntaxTokenKind.ElseKeyword,
                "select" => SyntaxTokenKind.SelectKeyword,
                "case" => SyntaxTokenKind.CaseKeyword,
                "break" => SyntaxTokenKind.BreakKeyword,
                "return" => SyntaxTokenKind.ReturnKeyword,
                _ => SyntaxTokenKind.EndOfFile,
            };
        }
    }
}
