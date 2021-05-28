using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace NitroSharp.NsScript.Syntax
{
    public enum LexingMode
    {
        Normal,
        DialogueBlock
    }

    internal sealed class Lexer : TextScanner
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MutableToken
        {
            public TextSpan TextSpan;
            public SyntaxTokenKind Kind;
            public SyntaxTokenFlags Flags;
        }

        private const string PRE_StartTag = "<pre>";
        private const string PRE_EndTag = "</pre>";

        private readonly LexingMode _initialMode;
        private readonly Stack<LexingMode> _lexingModeStack = new();
        private readonly DiagnosticBuilder _diagnostics = new();

        public Lexer(SourceText sourceText, LexingMode lexingMode = LexingMode.Normal)
            : base(sourceText.Source)
        {
            SourceText = sourceText;
            _initialMode = lexingMode;
            _lexingModeStack.Push(lexingMode);
        }

        public SourceText SourceText { get; }
        public DiagnosticBag Diagnostics => _diagnostics.ToImmutableBag();

        private LexingMode CurrentMode
            => _lexingModeStack.Count > 0 ? _lexingModeStack.Peek() : _initialMode;

        public void Lex(ref SyntaxToken syntaxToken)
        {
            ref MutableToken mutableTk = ref Unsafe.As<SyntaxToken, MutableToken>(ref syntaxToken);
            if (CurrentMode == LexingMode.DialogueBlock)
            {
                if (PeekChar() != '{' && !Match(PRE_EndTag))
                {
                    LexMarkupToken(ref mutableTk);
                    return;
                }
            }

            LexSyntaxToken(ref mutableTk);
            switch (mutableTk.Kind)
            {
                case SyntaxTokenKind.OpenBrace:
                    _lexingModeStack.Push(LexingMode.Normal);
                    break;
                case SyntaxTokenKind.CloseBrace:
                    if (_lexingModeStack.Count > 0)
                    {
                        _lexingModeStack.Pop();
                    }
                    break;
                case SyntaxTokenKind.DialogueBlockStartTag:
                    _lexingModeStack.Push(LexingMode.DialogueBlock);
                    break;
                case SyntaxTokenKind.DialogueBlockEndTag:
                    if (_lexingModeStack.Count > 0)
                    {
                        _lexingModeStack.Pop();
                    }
                    break;
            }
        }

        public SyntaxToken Lex()
        {
            SyntaxToken tk = default;
            ref MutableToken mutableTk = ref Unsafe.As<SyntaxToken, MutableToken>(ref tk);
            if (CurrentMode == LexingMode.DialogueBlock)
            {
                if (PeekChar() != '{' && !Match("</pre>"))
                {
                    LexMarkupToken(ref mutableTk);
                    Debug.Assert(tk.Kind != SyntaxTokenKind.None);
                    return tk;
                }
            }

            LexSyntaxToken(ref mutableTk);
            switch (mutableTk.Kind)
            {
                case SyntaxTokenKind.OpenBrace:
                    _lexingModeStack.Push(LexingMode.Normal);
                    break;

                case SyntaxTokenKind.CloseBrace:
                    if (_lexingModeStack.Count > 0)
                    {
                        _lexingModeStack.Pop();
                    }
                    break;

                case SyntaxTokenKind.DialogueBlockStartTag:
                    _lexingModeStack.Push(LexingMode.DialogueBlock);
                    break;

                case SyntaxTokenKind.DialogueBlockEndTag:
                    if (_lexingModeStack.Count > 0)
                    {
                        _lexingModeStack.Pop();
                    }
                    break;
            }

            Debug.Assert(tk.Kind != SyntaxTokenKind.None);
            return tk;
        }

        private void LexSyntaxToken(ref MutableToken token)
        {
            token = default;
            SkipSyntaxTrivia(isTrailing: false);
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '"':
                    if (!SyntaxFacts.IsSigil(PeekChar(1)))
                    {
                        ScanStringLiteralOrQuotedIdentifier(ref token, out TextSpan valueSpan);
                        ReadOnlySpan<char> value = SourceText.GetCharacterSpan(valueSpan);
                        // Certain keywords can appear in quotes
                        if (SyntaxFacts.TryGetKeywordKind(value, out SyntaxTokenKind keywordKind))
                        {
                            switch (keywordKind)
                            {
                                case SyntaxTokenKind.NullKeyword:
                                case SyntaxTokenKind.TrueKeyword:
                                case SyntaxTokenKind.FalseKeyword:
                                    token.Kind = keywordKind;
                                    break;
                            }

                        }
                    }
                    else
                    {
                        ScanStringLiteralOrQuotedIdentifier(ref token, out _);
                    }
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    if (!ScanDecNumericLiteral(ref token))
                    {
                        // If it's not a number, then it's an identifier starting with a number.
                        goto default;
                    }
                    break;

                case '$':
                    if (!ScanIdentifier(ref token))
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.Dollar;
                    }
                    break;

                case '#':
                    if (AdvanceIfMatches("#include"))
                    {
                        token.Kind = SyntaxTokenKind.IncludeDirective;
                    }
                    else if (!ScanHexTriplet(ref token) && !ScanIdentifier(ref token))
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.Hash;
                    }
                    break;

                case '@':
                    if (PeekChar(1) == '-' && PeekChar(2) == '>')
                    {
                        AdvanceChar(3);
                        token.Kind = SyntaxTokenKind.AtArrow;
                    }
                    else
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.At;
                    }
                    break;

                case '<':
                    char nextChar = PeekChar(1);
                    switch (nextChar)
                    {
                        case '=':
                            AdvanceChar(2);
                            token.Kind = SyntaxTokenKind.LessThanEquals;
                            break;

                        case 'p':
                        case 'P':
                            if (!ScanDialogueBlockStartTag(ref token))
                            {
                                goto default;
                            }
                            break;

                        case '/':
                            if (AdvanceIfMatches(PRE_EndTag))
                            {
                                token.Kind = SyntaxTokenKind.DialogueBlockEndTag;
                            }
                            else
                            {
                                goto default;
                            }
                            break;

                        default:
                            AdvanceChar();
                            token.Kind = SyntaxTokenKind.LessThan;
                            break;
                    }
                    break;

                case '{':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.OpenBrace;
                    break;

                case '}':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.CloseBrace;
                    break;

                case '(':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.OpenParen;
                    break;

                case ')':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.CloseParen;
                    break;

                case '.':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.Dot;
                    break;

                case ',':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.Comma;
                    break;

                case ':':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.Colon;
                    break;

                case ';':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.Semicolon;
                    break;

                case '=':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.EqualsEquals;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Equals;
                    }
                    break;

                case '+':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.PlusEquals;
                    }
                    else if (character == '+')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.PlusPlus;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Plus;
                    }
                    break;

                case '-':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.MinusEquals;
                    }
                    else if (character == '-')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.MinusMinus;
                    }
                    else if (character == '>')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.Arrow;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Minus;
                    }
                    break;

                case '*':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.AsteriskEquals;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Asterisk;
                    }
                    break;

                case '/':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.SlashEquals;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Slash;
                    }
                    break;

                case '%':
                    AdvanceChar();
                    token.Kind = SyntaxTokenKind.Percent;
                    break;

                case '>':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.GreaterThanEquals;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.GreaterThan;
                    }
                    break;

                case '!':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.ExclamationEquals;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Exclamation;
                    }
                    break;

                case '|':
                    AdvanceChar();
                    if (PeekChar() == '|')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.BarBar;
                    }
                    break;

                case '&':
                    AdvanceChar();
                    if (PeekChar() == '&')
                    {
                        AdvanceChar();
                        token.Kind = SyntaxTokenKind.AmpersandAmpersand;
                    }
                    else
                    {
                        token.Kind = SyntaxTokenKind.Ampersand;
                    }
                    break;

                case EofCharacter:
                    token.Kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    bool success = ScanIdentifier(ref token);
                    if (success)
                    {
                        ReadOnlySpan<char> text = SourceText.GetCharacterSpan(CurrentLexemeSpan);
                        if (SyntaxFacts.TryGetKeywordKind(text, out SyntaxTokenKind keywordKind))
                        {
                            token.Kind = keywordKind;
                        }
                    }
                    else
                    {
                        ScanBadToken(ref token);
                    }
                    break;
            }

            token.TextSpan = CurrentLexemeSpan;
            SkipSyntaxTrivia(isTrailing: true);
        }

        private void LexMarkupToken(ref MutableToken token)
        {
            bool skipTrailingTrivia = false;
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '[':
                    ScanDialogueBlockIdentifier(ref token);
                    skipTrailingTrivia = true;
                    break;

                case '\r':
                case '\n':
                    token.Kind = SyntaxTokenKind.MarkupBlankLine;
                    ScanEndOfLineSequence();
                    break;

                case EofCharacter:
                    token.Kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    ScanMarkup(ref token);
                    break;
            }

            token.TextSpan = CurrentLexemeSpan;
            if (skipTrailingTrivia)
            {
                SkipSyntaxTrivia(isTrailing: true);
            }
        }

        private bool ScanIdentifier(ref MutableToken token)
        {
            int start = Position;
            ScanSigil(ref token);

            if (!SyntaxFacts.IsIdentifierStartCharacter(PeekChar(), PeekChar(1)))
            {
                token.Flags = SyntaxTokenFlags.Empty;
                SetPosition(start);
                return false;
            }

            int valueStart = Position;
            while (SyntaxFacts.IsIdentifierPartCharacter(PeekChar(), PeekChar(1)))
            {
                AdvanceChar();
            }
            int valueEnd = Position;

            var valueSpan = new TextSpan(valueStart, valueEnd - valueStart);
            bool empty = valueSpan.Length == 0;
            if (empty)
            {
                token.Flags = SyntaxTokenFlags.Empty;
                SetPosition(start);
                return false;
            }

            token.Kind = SyntaxTokenKind.Identifier;
            return true;
        }

        private void ScanSigil(ref MutableToken token)
        {
            SyntaxTokenFlags flag = PeekChar() switch
            {
                '$' => SyntaxTokenFlags.HasDollarPrefix,
                '#' => SyntaxTokenFlags.HasHashPrefix,
                _ => SyntaxTokenFlags.Empty
            };
            if (flag != SyntaxTokenFlags.Empty)
            {
                AdvanceChar();
                token.Flags |= flag;
            }
        }

        private void ScanStringLiteralOrQuotedIdentifier(ref MutableToken token, out TextSpan valueSpan)
        {
            int start = Position;
            EatChar('"');
            if (PeekChar(1) != '"')
            {
                ScanSigil(ref token);
            }

            char c;
            while ((c = PeekChar()) != '"' && c != EofCharacter)
            {
                AdvanceChar();
            }

            int valueEnd = Position;
            if (!TryEatChar('"'))
            {
                Report(DiagnosticId.UnterminatedString, new TextSpan(start, 0));
            }

            token.Flags |= SyntaxTokenFlags.IsQuoted;
            token.Kind = SyntaxTokenKind.StringLiteralOrQuotedIdentifier;
            int valueStart = start + 1;
            valueSpan = new TextSpan(valueStart, valueEnd - valueStart);
        }

        private bool ScanDecNumericLiteral(ref MutableToken token)
        {
            bool isFloat = false;
            char c;
            while ((SyntaxFacts.IsDecDigit((c = PeekChar())) || c == '.'))
            {
                AdvanceChar();
                if (c == '.')
                {
                    token.Flags |= SyntaxTokenFlags.HasDecimalPoint;
                    isFloat = true;
                }
            }

            TextSpan valueSpan = CurrentLexemeSpan;
            ReadOnlySpan<char> valueText = SourceText.GetCharacterSpan(valueSpan);
            bool valid;
            if (!isFloat)
            {
                valid = int.TryParse(valueText, out _);
            }
            else
            {
                valid = float.TryParse(
                    valueText,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out _
                );
            }

            if (!valid)
            {
                Report(DiagnosticId.NumberTooLarge);
            }

            // If the next character is a valid identifier character,
            // then what we're scanning is actually an identifier that starts with a number
            // e.g "215_ＡＡルートグッドエンド".
            if (SyntaxFacts.IsIdentifierPartCharacter(PeekChar(), PeekChar(1)))
            {
                SetPosition(LexemeStart);
                return false;
            }

            token.Kind = SyntaxTokenKind.NumericLiteral;
            return true;
        }

        private bool ScanHexTriplet(ref MutableToken token)
        {
            int start = Position;
            EatChar('#');

            // We need exactly six digits.
            for (int i = 0; i < 6; i++)
            {
                if (!SyntaxFacts.IsHexDigit(PeekChar()))
                {
                    SetPosition(start);
                    return false;
                }

                AdvanceChar();
            }

            // If the next character can be part of an identifer, then what we're dealing with is not a hex triplet,
            // but rather an identifier prefixed with a '#', and it just so happens that its first 6 characters
            // are valid hex digits. '#ABCDEFghijklmno' would be an example of such an identifier.
            // NOTE: if the identifier is exactly 6 characters long, it will be treated as a hex triplet.
            // It isn't clear at the moment if there's a good solution for this.
            if (SyntaxFacts.IsIdentifierPartCharacter(PeekChar(), PeekChar(1)))
            {
                SetPosition(start);
                return false;
            }

            token.Kind = SyntaxTokenKind.NumericLiteral;
            token.Flags |= SyntaxTokenFlags.IsHexTriplet;
            return true;
        }

        private void ScanMarkup(ref MutableToken token)
        {
            int preNestingLevel = 0;

            char c;
            while ((c = PeekChar()) != '{' && c != EofCharacter)
            {
                if (c == '<')
                {
                    if (AdvanceIfMatches(PRE_StartTag))
                    {
                        preNestingLevel++;
                        continue;
                    }
                    if (Match(PRE_EndTag))
                    {
                        if (preNestingLevel == 0)
                        {
                            break;
                        }

                        AdvanceChar(PRE_EndTag.Length);
                        preNestingLevel--;
                        continue;
                    }
                }

                if (preNestingLevel == 0 && c == '/' && PeekChar(1) == '/')
                {
                    AdvanceChar(2);
                    ScanToEndOfLine();
                    ScanEndOfLine();
                    continue;
                }

                int pos = Position;
                int newlineSequenceLength = 0;
                while (SyntaxFacts.IsNewLine(PeekChar()))
                {
                    ScanEndOfLine();
                    if (++newlineSequenceLength == 2)
                    {
                        SetPosition(pos);
                        goto exit;
                    }
                }

                if (newlineSequenceLength == 0)
                {
                    AdvanceChar();
                }
            }

        exit:
            token.Kind = SyntaxTokenKind.Markup;
        }

        private bool ScanDialogueBlockStartTag(ref MutableToken token)
        {
            int start = Position;
            if (!AdvanceIfMatches("<pre "))
            {
                return false;
            }

            char c;
            while ((c = PeekChar()) != '>' && !IsEofOrNewLine(c))
            {
                AdvanceChar();
            }

            if (!TryEatChar('>'))
            {
                Report(DiagnosticId.UnterminatedDialogueBlockStartTag, new TextSpan(start, 0));
            }

            token.Kind = SyntaxTokenKind.DialogueBlockStartTag;
            return true;
        }

        private void ScanDialogueBlockIdentifier(ref MutableToken token)
        {
            int start = Position;
            EatChar('[');

            char c;
            while ((c = PeekChar()) != ']' && !IsEofOrNewLine(c))
            {
                AdvanceChar();
            }

            if (!TryEatChar(']'))
            {
                Report(DiagnosticId.UnterminatedDialogueBlockIdentifier, new TextSpan(start, 0));
            }

            token.Kind = SyntaxTokenKind.DialogueBlockIdentifier;
        }

        private void ScanBadToken(ref MutableToken token)
        {
            while (!IsEofOrNewLine(PeekChar()))
            {
                AdvanceChar();
            }

            token.Kind = SyntaxTokenKind.BadToken;
        }

        private void SkipSyntaxTrivia(bool isTrailing)
        {
            StartScanning();
            bool trivia = true;
            do
            {
                char character = PeekChar();
                if (SyntaxFacts.IsWhitespace(character))
                {
                    ScanWhitespace();
                    continue;
                }

                if (SyntaxFacts.IsNewLine(character))
                {
                    ScanEndOfLine();
                    if (isTrailing) { break; }
                    continue;
                }

                switch (character)
                {
                    case '/':
                        if ((character = PeekChar(1)) == '/')
                        {
                            ScanToEndOfLine();
                        }
                        else if (character == '*')
                        {
                            ScanMultiLineComment();
                        }
                        else
                        {
                            trivia = false;
                        }
                        break;

                    case '.':
                    case '>':
                        // The following character sequences are treated as "//":
                        // ".//"
                        // ">//"
                        // ".."
                        if (PeekChar(1) == '/' && PeekChar(2) == '/'
                            || character == '.' && PeekChar(1) == '.')
                        {
                            ScanToEndOfLine();
                        }
                        else
                        {
                            trivia = false;
                        }
                        break;

                    default:
                        trivia = false;
                        break;
                }
            } while (trivia);
        }

        private void ScanMultiLineComment()
        {
            char c;
            bool isInsideQuotes = false;
            while (!((c = PeekChar()) == '*' && PeekChar(1) == '/') || isInsideQuotes)
            {
                if (c == EofCharacter)
                {
                    Report(DiagnosticId.UnterminatedComment, CurrentSpanStart);
                    return;
                }
                if (c == '"')
                {
                    isInsideQuotes = !isInsideQuotes;
                }
                AdvanceChar();
            }

            AdvanceChar(2); // "*/"
        }

        private static bool IsEofOrNewLine(char c) => c switch
        {
            EofCharacter or '\r' or '\n' => true,
            _ => false,
        };

        private void Report(DiagnosticId diagnosticId) => Report(diagnosticId, CurrentLexemeSpan);
        private void Report(DiagnosticId diagnosticId, TextSpan textSpan)
        {
            _diagnostics.Report(diagnosticId, textSpan);
        }
    }
}
