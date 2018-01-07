using NitroSharp.NsScript.Uitls;
using System.Globalization;

namespace NitroSharp.NsScript.Syntax
{
    public static class SyntaxFacts
    {
        public static bool IsLetter(char c) => char.IsLetter(c);
        public static bool IsLatinLetter(char c) => c >= 'A' && c <= 'z';
        public static bool IsDecDigit(char c) => c >= '0' && c <= '9';
        public static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        public static bool IsWhitespace(char c)
        {
            // SPACE
            // CHARACTER TABULATION (U+0009)
            // LINE TABULATION (U+000B)
            // FORM FEED (U+000C)
            // Any other character with Unicode class Zs

            return c == ' '
                || c == '\t'
                || c == '\v'
                || c == '\f'
                || c == '\u001A'
                || (c > 255 && CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator);
        }

        public static bool IsNewLine(char c)
        {
            // CR, LF
            // NEXT LINE (U+0085)
            // LINE SEPARATOR (U+2028)
            // PARAGRAPH SEPARATOR (U+2029)

            return c == '\r'
                || c == '\n'
                || c == '\u0085'
                || c == '\u2028'
                || c == '\u2029';
        }

        public static bool IsSigil(char c)
        {
            switch (c)
            {
                case '$':
                case '#':
                case '@':
                    return true;

                default:
                    return false;
            }
        }

        public static SyntaxTokenKind GetKeywordKind(string keyword)
        {
            switch (keyword)
            {
                case "include": return SyntaxTokenKind.IncludeKeyword;
                case "chapter": return SyntaxTokenKind.ChapterKeyword;
                case "function": return SyntaxTokenKind.FunctionKeyword;
                case "scene": return SyntaxTokenKind.SceneKeyword;
                case "call_scene": return SyntaxTokenKind.CallSceneKeyword;
                case "call_chapter": return SyntaxTokenKind.CallChapterKeyword;
                case "null": return SyntaxTokenKind.NullKeyword;
                case "Null": return SyntaxTokenKind.NullKeyword;
                case "NULL": return SyntaxTokenKind.NullKeyword;
                case "true": return SyntaxTokenKind.TrueKeyword;
                case "True": return SyntaxTokenKind.TrueKeyword;
                case "TRUE": return SyntaxTokenKind.TrueKeyword;
                case "false": return SyntaxTokenKind.FalseKeyword;
                case "False": return SyntaxTokenKind.FalseKeyword;
                case "FALSE": return SyntaxTokenKind.FalseKeyword;
                case "while": return SyntaxTokenKind.WhileKeyword;
                case "if": return SyntaxTokenKind.IfKeyword;
                case "else": return SyntaxTokenKind.ElseKeyword;
                case "select": return SyntaxTokenKind.SelectKeyword;
                case "case": return SyntaxTokenKind.CaseKeyword;
                case "break": return SyntaxTokenKind.BreakKeyword;
                case "return": return SyntaxTokenKind.ReturnKeyword;

                default:
                    return SyntaxTokenKind.None;
            }
        }

        public static bool IsIdentifierStopCharacter(char c) => !IsIdentifierPartCharacter(c);
        public static bool IsIdentifierPartCharacter(char c)
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
                case '-':
                case '*':
                case '<':
                case '>':
                case '%':
                case '!':
                case '|':
                case '&':
                    return false;

                default:
                    return true;
            }
        }

        public static bool IsPrefixUnaryOperator(SyntaxTokenKind tokenKind) => TryGetPrefixUnaryOperatorKind(tokenKind, out var _);
        public static bool IsPostfixUnaryOperator(SyntaxTokenKind tokenKind) => TryGetPostfixUnaryOperatorKind(tokenKind, out var _);
        public static bool IsBinaryOperator(SyntaxTokenKind tokenKind) => TryGetBinaryOperatorKind(tokenKind, out var _);
        public static bool IsAssignmentOperator(SyntaxTokenKind tokenKind) => TryGetAssignmentOperatorKind(tokenKind, out var _);

        public static UnaryOperatorKind GetPrefixUnaryOperatorKind(SyntaxTokenKind operatorTokenKind)
        {
            bool success = TryGetPrefixUnaryOperatorKind(operatorTokenKind, out var kind);
            return success ? kind : throw ExceptionUtils.UnexpectedValue(nameof(operatorTokenKind));
        }

        public static UnaryOperatorKind GetPostfixUnaryOperatorKind(SyntaxTokenKind operatorTokenKind)
        {
            bool success = TryGetPostfixUnaryOperatorKind(operatorTokenKind, out var kind);
            return success ? kind : throw ExceptionUtils.UnexpectedValue(nameof(operatorTokenKind));
        }

        public static BinaryOperatorKind GetBinaryOperatorKind(SyntaxTokenKind operatorTokenKind)
        {
            bool success = TryGetBinaryOperatorKind(operatorTokenKind, out var kind);
            return success ? kind : throw ExceptionUtils.UnexpectedValue(nameof(operatorTokenKind));
        }

        public static AssignmentOperatorKind GetAssignmentOperatorKind(SyntaxTokenKind operatorTokenKind)
        {
            bool success = TryGetAssignmentOperatorKind(operatorTokenKind, out var kind);
            return success ? kind : throw ExceptionUtils.UnexpectedValue(nameof(operatorTokenKind));
        }

        public static bool TryGetPrefixUnaryOperatorKind(SyntaxTokenKind operatorTokenKind, out UnaryOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.ExclamationToken:
                    kind = UnaryOperatorKind.Not;
                    break;
                case SyntaxTokenKind.PlusToken:
                    kind = UnaryOperatorKind.Plus;
                    break;
                case SyntaxTokenKind.MinusToken:
                    kind = UnaryOperatorKind.Minus;
                    break;

                default:
                    kind = default(UnaryOperatorKind);
                    return false;
            }

            return true;
        }

        public static bool TryGetPostfixUnaryOperatorKind(SyntaxTokenKind operatorTokenKind, out UnaryOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.PlusPlusToken:
                    kind = UnaryOperatorKind.PostfixIncrement;
                    break;
                case SyntaxTokenKind.MinusMinusToken:
                    kind = UnaryOperatorKind.PostfixDecrement;
                    break;

                default:
                    kind = default(UnaryOperatorKind);
                    return false;
            }

            return true;
        }

        public static bool TryGetBinaryOperatorKind(SyntaxTokenKind operatorTokenKind, out BinaryOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.PlusToken:
                    kind = BinaryOperatorKind.Add;
                    break;
                case SyntaxTokenKind.MinusToken:
                    kind = BinaryOperatorKind.Subtract;
                    break;
                case SyntaxTokenKind.AsteriskToken:
                    kind = BinaryOperatorKind.Multiply;
                    break;
                case SyntaxTokenKind.SlashToken:
                    kind = BinaryOperatorKind.Divide;
                    break;
                case SyntaxTokenKind.PercentToken:
                    kind = BinaryOperatorKind.Remainder;
                    break;
                case SyntaxTokenKind.LessThanToken:
                    kind = BinaryOperatorKind.LessThan;
                    break;
                case SyntaxTokenKind.LessThanEqualsToken:
                    kind = BinaryOperatorKind.LessThanOrEqual;
                    break;
                case SyntaxTokenKind.GreaterThanToken:
                    kind = BinaryOperatorKind.GreaterThan;
                    break;
                case SyntaxTokenKind.GreaterThanEqualsToken:
                    kind = BinaryOperatorKind.GreaterThanOrEqual;
                    break;
                case SyntaxTokenKind.BarBarToken:
                    kind = BinaryOperatorKind.Or;
                    break;
                case SyntaxTokenKind.AmpersandAmpersandToken:
                    kind = BinaryOperatorKind.And;
                    break;
                case SyntaxTokenKind.EqualsEqualsToken:
                    kind = BinaryOperatorKind.Equals;
                    break;
                case SyntaxTokenKind.ExclamationEqualsToken:
                    kind = BinaryOperatorKind.NotEquals;
                    break;

                default:
                    kind = default(BinaryOperatorKind);
                    return false;
            }

            return true;
        }

        public static bool TryGetAssignmentOperatorKind(SyntaxTokenKind operatorTokenKind, out AssignmentOperatorKind kind)
        {
            switch (operatorTokenKind)
            {
                case SyntaxTokenKind.EqualsToken:
                    kind = AssignmentOperatorKind.Assign;
                    break;
                case SyntaxTokenKind.PlusEqualsToken:
                    kind = AssignmentOperatorKind.AddAssign;
                    break;
                case SyntaxTokenKind.MinusEqualsToken:
                    kind = AssignmentOperatorKind.SubtractAssign;
                    break;
                case SyntaxTokenKind.AsteriskEqualsToken:
                    kind = AssignmentOperatorKind.MultiplyAssign;
                    break;
                case SyntaxTokenKind.SlashEqualsToken:
                    kind = AssignmentOperatorKind.DivideAssign;
                    break;

                default:
                    kind = default(AssignmentOperatorKind);
                    return false;
            }

            return true;
        }

        public static string GetText(SyntaxTokenKind kind)
        {
            switch (kind)
            {
                case SyntaxTokenKind.ExclamationToken:
                    return "!";
                case SyntaxTokenKind.AmpersandToken:
                    return "&";
                case SyntaxTokenKind.AsteriskToken:
                    return "*";
                case SyntaxTokenKind.OpenParenToken:
                    return "(";
                case SyntaxTokenKind.CloseParenToken:
                    return ")";
                case SyntaxTokenKind.MinusToken:
                    return "-";
                case SyntaxTokenKind.PlusToken:
                    return "+";
                case SyntaxTokenKind.EqualsToken:
                    return "=";
                case SyntaxTokenKind.OpenBraceToken:
                    return "{";
                case SyntaxTokenKind.CloseBraceToken:
                    return "}";
                case SyntaxTokenKind.ColonToken:
                    return ":";
                case SyntaxTokenKind.SemicolonToken:
                    return ";";
                case SyntaxTokenKind.LessThanToken:
                    return "<";
                case SyntaxTokenKind.CommaToken:
                    return ",";
                case SyntaxTokenKind.GreaterThanToken:
                    return ">";
                case SyntaxTokenKind.DotToken:
                    return ".";
                case SyntaxTokenKind.SlashToken:
                    return "/";
                case SyntaxTokenKind.PercentToken:
                    return "%";

                // compound
                case SyntaxTokenKind.BarBarToken:
                    return "||";
                case SyntaxTokenKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxTokenKind.MinusMinusToken:
                    return "--";
                case SyntaxTokenKind.PlusPlusToken:
                    return "++";
                case SyntaxTokenKind.ExclamationEqualsToken:
                    return "!=";
                case SyntaxTokenKind.EqualsEqualsToken:
                    return "==";
                case SyntaxTokenKind.LessThanEqualsToken:
                    return "<=";
                case SyntaxTokenKind.GreaterThanEqualsToken:
                    return ">=";
                case SyntaxTokenKind.SlashEqualsToken:
                    return "/=";
                case SyntaxTokenKind.AsteriskEqualsToken:
                    return "*=";
                case SyntaxTokenKind.PlusEqualsToken:
                    return "+=";
                case SyntaxTokenKind.MinusEqualsToken:
                    return "-=";

                case SyntaxTokenKind.IncludeKeyword:
                    return "include";
                case SyntaxTokenKind.ChapterKeyword:
                    return "chapter";
                case SyntaxTokenKind.FunctionKeyword:
                    return "function";
                case SyntaxTokenKind.SceneKeyword:
                    return "scene";
                case SyntaxTokenKind.CallSceneKeyword:
                    return "call_scene";
                case SyntaxTokenKind.CallChapterKeyword:
                    return "call_chapter";
                case SyntaxTokenKind.NullKeyword:
                    return "null";
                case SyntaxTokenKind.TrueKeyword:
                    return "true";
                case SyntaxTokenKind.FalseKeyword:
                    return "false";
                case SyntaxTokenKind.WhileKeyword:
                    return "while";
                case SyntaxTokenKind.IfKeyword:
                    return "if";
                case SyntaxTokenKind.ElseKeyword:
                    return "else";
                case SyntaxTokenKind.SelectKeyword:
                    return "select";
                case SyntaxTokenKind.CaseKeyword:
                    return "case";
                case SyntaxTokenKind.BreakKeyword:
                    return "break";

                default:
                    return string.Empty;
            }
        }
    }
}
