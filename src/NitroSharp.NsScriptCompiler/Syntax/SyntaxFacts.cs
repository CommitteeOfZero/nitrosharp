using NitroSharp.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew.Syntax
{
    public static class SyntaxFacts
    {
        private const char EofCharacter = char.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLetter(char c) => char.IsLetter(c);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLatinLetter(char c) => c >= 'A' && c <= 'z';
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
            return kind == SyntaxNodeKind.AssignmentExpression || kind == SyntaxNodeKind.FunctionCall;
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
                    kind = default(BinaryOperatorKind);
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
                    kind = default(AssignmentOperatorKind);
                    return false;
            }

            return true;
        }

        public static string GetText(SyntaxTokenKind kind)
        {
            switch (kind)
            {
                case SyntaxTokenKind.Dollar:
                    return "$";
                case SyntaxTokenKind.Hash:
                    return "#";
                case SyntaxTokenKind.At:
                    return "@";
                case SyntaxTokenKind.Exclamation:
                    return "!";
                case SyntaxTokenKind.Ampersand:
                    return "&";
                case SyntaxTokenKind.Asterisk:
                    return "*";
                case SyntaxTokenKind.OpenParen:
                    return "(";
                case SyntaxTokenKind.CloseParen:
                    return ")";
                case SyntaxTokenKind.Minus:
                    return "-";
                case SyntaxTokenKind.Plus:
                    return "+";
                case SyntaxTokenKind.Equals:
                    return "=";
                case SyntaxTokenKind.OpenBrace:
                    return "{";
                case SyntaxTokenKind.CloseBrace:
                    return "}";
                case SyntaxTokenKind.Colon:
                    return ":";
                case SyntaxTokenKind.Semicolon:
                    return ";";
                case SyntaxTokenKind.LessThan:
                    return "<";
                case SyntaxTokenKind.Comma:
                    return ",";
                case SyntaxTokenKind.GreaterThan:
                    return ">";
                case SyntaxTokenKind.Dot:
                    return ".";
                case SyntaxTokenKind.Slash:
                    return "/";
                case SyntaxTokenKind.Percent:
                    return "%";
                case SyntaxTokenKind.Arrow:
                    return "->";
                case SyntaxTokenKind.AtArrow:
                    return "@->";

                // compound
                case SyntaxTokenKind.BarBar:
                    return "||";
                case SyntaxTokenKind.AmpersandAmpersand:
                    return "&&";
                case SyntaxTokenKind.MinusMinus:
                    return "--";
                case SyntaxTokenKind.PlusPlus:
                    return "++";
                case SyntaxTokenKind.ExclamationEquals:
                    return "!=";
                case SyntaxTokenKind.EqualsEquals:
                    return "==";
                case SyntaxTokenKind.LessThanEquals:
                    return "<=";
                case SyntaxTokenKind.GreaterThanEquals:
                    return ">=";
                case SyntaxTokenKind.SlashEquals:
                    return "/=";
                case SyntaxTokenKind.AsteriskEquals:
                    return "*=";
                case SyntaxTokenKind.PlusEquals:
                    return "+=";
                case SyntaxTokenKind.MinusEquals:
                    return "-=";

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
                case SyntaxTokenKind.ReturnKeyword:
                    return "return";

                case SyntaxTokenKind.IncludeDirective:
                    return "#include";

                case SyntaxTokenKind.PXmlLineSeparator:
                    return "\r\n";
                case SyntaxTokenKind.DialogueBlockEndTag:
                    return "</PRE>";

                default:
                    return string.Empty;
            }
        }

        internal static class KeywordScanner
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
                int hash = HashHelper.GetFNVHashCode(text);
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
