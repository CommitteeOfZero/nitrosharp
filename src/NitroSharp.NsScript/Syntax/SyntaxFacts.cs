using NitroSharp.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Syntax
{
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
        public static bool IsNewLine(char c)
        {
            return c == '\r' || c == '\n';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSigil(char c)
        {
            return c is '$' or '#' or '@';
        }

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
            return tokenKind == SyntaxTokenKind.Semicolon || tokenKind == SyntaxTokenKind.Colon;
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

        public static bool IsStatementExpression(Expression expression)
        {
            SyntaxNodeKind kind = expression.Kind;
            return kind == SyntaxNodeKind.AssignmentExpression || kind == SyntaxNodeKind.FunctionCallExpression;
        }

        public static bool TryGetUnaryOperatorKind(SyntaxTokenKind operatorTokenKind, out UnaryOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.Exclamation:
                    kind = UnaryOperatorKind.Not;
                    break;
                case SyntaxTokenKind.Plus:
                    kind = UnaryOperatorKind.Plus;
                    break;
                case SyntaxTokenKind.Minus:
                    kind = UnaryOperatorKind.Minus;
                    break;
                case SyntaxTokenKind.At:
                    kind = UnaryOperatorKind.Delta;
                    break;

                default:
                    kind = default;
                    return false;
            }

            return true;
        }

        public static bool TryGetBinaryOperatorKind(SyntaxTokenKind operatorTokenKind, out BinaryOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.Plus:
                    kind = BinaryOperatorKind.Add;
                    break;
                case SyntaxTokenKind.Minus:
                    kind = BinaryOperatorKind.Subtract;
                    break;
                case SyntaxTokenKind.Asterisk:
                    kind = BinaryOperatorKind.Multiply;
                    break;
                case SyntaxTokenKind.Slash:
                    kind = BinaryOperatorKind.Divide;
                    break;
                case SyntaxTokenKind.Percent:
                    kind = BinaryOperatorKind.Remainder;
                    break;
                case SyntaxTokenKind.LessThan:
                    kind = BinaryOperatorKind.LessThan;
                    break;
                case SyntaxTokenKind.LessThanEquals:
                    kind = BinaryOperatorKind.LessThanOrEqual;
                    break;
                case SyntaxTokenKind.GreaterThan:
                    kind = BinaryOperatorKind.GreaterThan;
                    break;
                case SyntaxTokenKind.GreaterThanEquals:
                    kind = BinaryOperatorKind.GreaterThanOrEqual;
                    break;
                case SyntaxTokenKind.BarBar:
                    kind = BinaryOperatorKind.Or;
                    break;
                case SyntaxTokenKind.AmpersandAmpersand:
                    kind = BinaryOperatorKind.And;
                    break;
                case SyntaxTokenKind.EqualsEquals:
                    kind = BinaryOperatorKind.Equals;
                    break;
                case SyntaxTokenKind.ExclamationEquals:
                    kind = BinaryOperatorKind.NotEquals;
                    break;

                default:
                    kind = default;
                    return false;
            }

            return true;
        }

        public static bool TryGetAssignmentOperatorKind(SyntaxTokenKind operatorTokenKind, out AssignmentOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.Equals:
                    kind = AssignmentOperatorKind.Assign;
                    break;
                case SyntaxTokenKind.PlusEquals:
                    kind = AssignmentOperatorKind.AddAssign;
                    break;
                case SyntaxTokenKind.MinusEquals:
                    kind = AssignmentOperatorKind.SubtractAssign;
                    break;
                case SyntaxTokenKind.AsteriskEquals:
                    kind = AssignmentOperatorKind.MultiplyAssign;
                    break;
                case SyntaxTokenKind.SlashEquals:
                    kind = AssignmentOperatorKind.DivideAssign;
                    break;
                case SyntaxTokenKind.PlusPlus:
                    kind = AssignmentOperatorKind.Increment;
                    break;
                case SyntaxTokenKind.MinusMinus:
                    kind = AssignmentOperatorKind.Decrement;
                    break;

                default:
                    kind = default;
                    return false;
            }

            return true;
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

                _ => string.Empty,
            };
        }

        private static class KeywordScanner
        {
            public static bool TryRecognizeKeyword(ReadOnlySpan<char> text, out SyntaxTokenKind keywordKind)
                => (keywordKind = RecognizeKeyword(text)) != SyntaxTokenKind.None;

            public static SyntaxTokenKind RecognizeKeyword(ReadOnlySpan<char> text)
            {
                return text switch
                {
                    "chapter" => SyntaxTokenKind.ChapterKeyword,
                    "function" => SyntaxTokenKind.FunctionKeyword,
                    "scene" => SyntaxTokenKind.SceneKeyword,
                    "call_scene" => SyntaxTokenKind.CallSceneKeyword,
                    "call_chapter" => SyntaxTokenKind.CallChapterKeyword,
                    "null" => SyntaxTokenKind.NullKeyword,
                    "Null" => SyntaxTokenKind.NullKeyword,
                    "NULL" => SyntaxTokenKind.NullKeyword,
                    "true" => SyntaxTokenKind.TrueKeyword,
                    "True" => SyntaxTokenKind.TrueKeyword,
                    "TRUE" => SyntaxTokenKind.TrueKeyword,
                    "false" => SyntaxTokenKind.FalseKeyword,
                    "False" => SyntaxTokenKind.FalseKeyword,
                    "FALSE" => SyntaxTokenKind.FalseKeyword,
                    "while" => SyntaxTokenKind.WhileKeyword,
                    "if" => SyntaxTokenKind.IfKeyword,
                    "else" => SyntaxTokenKind.ElseKeyword,
                    "select" => SyntaxTokenKind.SelectKeyword,
                    "case" => SyntaxTokenKind.CaseKeyword,
                    "break" => SyntaxTokenKind.BreakKeyword,
                    "return" => SyntaxTokenKind.ReturnKeyword,

                    _ => SyntaxTokenKind.None,
                };
            }
        }
    }
}
