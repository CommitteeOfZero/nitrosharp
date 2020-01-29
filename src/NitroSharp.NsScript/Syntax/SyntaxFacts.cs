using NitroSharp.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Syntax
{
    public static class SyntaxFacts
    {
        private const char EofCharacter = char.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDecDigit(char c) => c >= '0' && c <= '9';
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
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
            return c == '$' || c == '#' || c == '@';
        }

        public static bool TryGetKeywordKind(ReadOnlySpan<char> text, out SyntaxTokenKind kind)
            => KeywordScanner.TryRecognizeKeyword(text, out kind);

        public static SyntaxTokenKind GetKeywordKind(ReadOnlySpan<char> text)
            => KeywordScanner.RecognizeKeyword(text);

        public static bool IsIdentifierStartCharacter(char c)
            => IsIdentifierPartCharacter(c) && !IsDecDigit(c);

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

        public static bool IsStatementExpression(ExpressionSyntax expression)
        {
            var kind = expression.Kind;
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

                SyntaxTokenKind.PXmlLineSeparator => "\r\n",
                SyntaxTokenKind.DialogueBlockEndTag => "</PRE>",

                _ => string.Empty,
            };
        }

        private static class KeywordScanner
        {
            private const int ChapterKeywordHash = 703916114;
            private const int FunctionKeywordHash = -1630125495;
            private const int SceneKeywordHash = 543410963;
            private const int CallSceneKeywordHash = 731995282;
            private const int CallChapterKeywordHash = 131528059;
            private const int NullKeywordHash = 1996966820;
            private const int PascalCaseNullKeywordHash = -147613756;
            private const int CapitalizedNullKeywordHash = 963632676;
            private const int TrueKeywordHash = 1303515621;
            private const int PascalCaseTrueKeywordHash = -841064955;
            private const int CapitalizedTrueKeywordHash = 1343949093;
            private const int MisspelledTrueKeywordHash = -2008300465;
            private const int FalseKeywordHash = 184981848;
            private const int PascalCaseFalseKeywordHash = -1753917960;
            private const int CapitalizedFalseKeywordHash = -296126344;
            private const int WhileKeywordHash = 231090382;
            private const int IfKeywordHash = 959999494;
            private const int ElseKeywordHash = -1111532560;
            private const int SelectKeywordHash = 297952813;
            private const int CaseKeywordHash = -1692059471;
            private const int BreakKeywordHash = -916160136;
            private const int ReturnKeywordHash = -2047985729;

            public static bool TryRecognizeKeyword(ReadOnlySpan<char> text, out SyntaxTokenKind keywordKind)
                => (keywordKind = RecognizeKeyword(text)) != SyntaxTokenKind.None;

            public static SyntaxTokenKind RecognizeKeyword(ReadOnlySpan<char> text)
            {
                int hash = FnvHasher.HashString(text);
                switch (hash)
                {
                    case ChapterKeywordHash: return SyntaxTokenKind.ChapterKeyword;
                    case FunctionKeywordHash: return SyntaxTokenKind.FunctionKeyword;
                    case SceneKeywordHash: return SyntaxTokenKind.SceneKeyword;
                    case CallSceneKeywordHash: return SyntaxTokenKind.CallSceneKeyword;
                    case CallChapterKeywordHash: return SyntaxTokenKind.CallChapterKeyword;
                    case NullKeywordHash:
                    case PascalCaseNullKeywordHash:
                    case CapitalizedNullKeywordHash:
                        return SyntaxTokenKind.NullKeyword;
                    case TrueKeywordHash:
                    case PascalCaseTrueKeywordHash:
                    case CapitalizedTrueKeywordHash:
                    case MisspelledTrueKeywordHash:
                        return SyntaxTokenKind.TrueKeyword;
                    case FalseKeywordHash:
                    case PascalCaseFalseKeywordHash:
                    case CapitalizedFalseKeywordHash:
                        return SyntaxTokenKind.FalseKeyword;
                    case WhileKeywordHash: return SyntaxTokenKind.WhileKeyword;
                    case IfKeywordHash: return SyntaxTokenKind.IfKeyword;
                    case ElseKeywordHash: return SyntaxTokenKind.ElseKeyword;
                    case SelectKeywordHash: return SyntaxTokenKind.SelectKeyword;
                    case CaseKeywordHash: return SyntaxTokenKind.CaseKeyword;
                    case BreakKeywordHash: return SyntaxTokenKind.BreakKeyword;
                    case ReturnKeywordHash: return SyntaxTokenKind.ReturnKeyword;

                    default:
                        return SyntaxTokenKind.None;
                }
            }
        }
    }
}
