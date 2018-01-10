using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    internal sealed class Lexer : TextScanner
    {
        private struct TokenInfo
        {
            public SyntaxTokenKind Kind;
            public string Text;
            public string StringValue;
            public double DoubleValue;
            public SigilKind SigilKind;
        }

        private const string PRE_StartTag = "<pre>";
        private const string PRE_EndTag = "</pre>";

        private readonly LexingMode _initialMode;
        private readonly Stack<LexingMode> _lexingModeStack = new Stack<LexingMode>();

        public Lexer(SourceText sourceText, LexingMode lexingMode = LexingMode.Normal) : base(sourceText.Source)
        {
            SourceText = sourceText;
            _initialMode = lexingMode;
            _lexingModeStack.Push(lexingMode);
        }

        public SourceText SourceText { get; }

        private LexingMode CurrentMode
        {
            get => _lexingModeStack.Count > 0 ? _lexingModeStack.Peek() : _initialMode;
        }

        public SyntaxToken Lex()
        {
            if (CurrentMode == LexingMode.Paragraph)
            {
                if (PeekChar() != '{' && !Is_PRE_EndTag())
                {
                    return LexPXmlToken();
                }
            }

            var token = LexSyntaxToken();
            switch (token.Kind)
            {
                case SyntaxTokenKind.OpenBraceToken:
                    _lexingModeStack.Push(LexingMode.Normal);
                    break;

                case SyntaxTokenKind.CloseBraceToken:
                    _lexingModeStack.Pop();
                    break;

                case SyntaxTokenKind.FunctionKeyword:
                    _lexingModeStack.Push(LexingMode.ParameterList);
                    break;

                case SyntaxTokenKind.CloseParenToken:
                    if (CurrentMode == LexingMode.ParameterList)
                    {
                        _lexingModeStack.Pop();
                    }
                    break;

                case SyntaxTokenKind.ParagraphStartTag:
                    _lexingModeStack.Push(LexingMode.Paragraph);
                    break;

                case SyntaxTokenKind.ParagraphEndTag:
                    _lexingModeStack.Pop();
                    break;
            }

            return token;
        }

        private SyntaxToken LexSyntaxToken()
        {
            SkipSyntaxTrivia(isTrailing: false);

            var info = new TokenInfo();
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '"':
                    if (CurrentMode != LexingMode.ParameterList && PeekChar(1) != '$')
                    {
                        ScanStringLiteral(ref info);
                        // Could actually be a quoted keyword (e.g "null")
                        info.Kind = SyntaxFacts.GetKeywordKind(info.StringValue);
                        if (info.Kind == SyntaxTokenKind.None)
                        {
                            info.Kind = SyntaxTokenKind.StringLiteralToken;
                        }
                    }
                    else
                    {
                        ScanIdentifier(ref info);
                        info.Kind = SyntaxTokenKind.IdentifierToken;
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
                    ScanNumericLiteral(ref info);
                    info.Kind = SyntaxTokenKind.NumericLiteralToken;
                    break;

                case '$':
                    ScanIdentifier(ref info);
                    info.Kind = SyntaxTokenKind.IdentifierToken;
                    break;

                case '#':
                    if (IsIncludeDirective())
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.HashToken;
                    }
                    else if (IsHexNumericLiteral())
                    {
                        ScanNumericLiteral(ref info);
                        info.Kind = SyntaxTokenKind.NumericLiteralToken;
                    }
                    else
                    {
                        ScanIdentifier(ref info);
                        info.Kind = SyntaxTokenKind.IdentifierToken;
                    }
                    break;

                case '@':
                    char next = PeekChar(1);
                    if (PeekChar(1) == '-' && PeekChar(2) == '>')
                    {
                        ScanIdentifier(ref info);
                        info.Kind = SyntaxTokenKind.IdentifierToken;
                    }
                    else if (next == '+' || next == '-' || SyntaxFacts.IsDecDigit(next) || SyntaxFacts.IsSigil(next))
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.AtToken;
                    }
                    else
                    {
                        ScanIdentifier(ref info);
                        info.Kind = SyntaxTokenKind.IdentifierToken;
                    }
                    break;

                case '<':
                    char nextChar = PeekChar(1);
                    switch (nextChar)
                    {
                        case '=':
                            AdvanceChar(2);
                            info.Kind = SyntaxTokenKind.LessThanEqualsToken;
                            break;

                        case 'p':
                        case 'P':
                            ScanParagraphStartTag(ref info);
                            info.Kind = SyntaxTokenKind.ParagraphStartTag;
                            break;

                        case '/':
                            AdvanceChar(PRE_EndTag.Length);
                            info.Kind = SyntaxTokenKind.ParagraphEndTag;
                            info.Text = "</PRE>";
                            break;

                        default:
                            AdvanceChar();
                            info.Kind = SyntaxTokenKind.LessThanToken;
                            break;
                    }
                    break;

                case '{':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.OpenBraceToken;
                    break;

                case '}':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.CloseBraceToken;
                    break;

                case '(':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.OpenParenToken;
                    break;

                case ')':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.CloseParenToken;
                    break;

                case '.':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.DotToken;
                    break;

                case ',':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.CommaToken;
                    break;

                case ':':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.ColonToken;
                    break;

                case ';':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.SemicolonToken;
                    break;

                case '=':
                    AdvanceChar();
                    if ((PeekChar()) == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.EqualsEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.EqualsToken;
                    }
                    break;

                case '+':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.PlusEqualsToken;
                    }
                    else if (character == '+')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.PlusPlusToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.PlusToken;
                    }
                    break;

                case '-':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.MinusEqualsToken;
                    }
                    else if (character == '-')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.MinusMinusToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.MinusToken;
                    }
                    break;

                case '*':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.AsteriskEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.AsteriskToken;
                    }
                    break;

                case '/':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.SlashEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.SlashToken;
                    }
                    break;

                case '%':
                    AdvanceChar();
                    info.Kind = SyntaxTokenKind.PercentToken;
                    break;

                case '>':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.GreaterThanEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.GreaterThanToken;
                    }
                    break;

                case '!':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.ExclamationEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.ExclamationToken;
                    }
                    break;

                case '|':
                    AdvanceChar();
                    if (PeekChar() == '|')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.BarBarToken;
                    }
                    break;

                case '&':
                    AdvanceChar();
                    if (PeekChar() == '&')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.AmpersandAmpersandToken;
                    }
                    else
                    {
                        info.Kind = SyntaxTokenKind.AmpersandToken;
                    }
                    break;

                case EofCharacter:
                    info.Kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    bool success = ScanIdentifier(ref info);
                    if (success)
                    {
                        info.Kind = SyntaxFacts.GetKeywordKind(info.Text);
                        if (info.Kind == SyntaxTokenKind.None)
                        {
                            info.Kind = SyntaxTokenKind.IdentifierToken;
                        }
                    }
                    else
                    {
                        ScanBadToken();
                        info.Kind = SyntaxTokenKind.BadToken;
                    }
                    break;
            }

            var token = CreateToken(ref info);
            SkipSyntaxTrivia(isTrailing: true);
            return token;
        }

        private SyntaxToken CreateToken(ref TokenInfo tokenInfo)
        {
            var span = GetCurrentLexemeSpan();
            switch (tokenInfo.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                    return SyntaxToken.Identifier(tokenInfo.Text, span, tokenInfo.StringValue, tokenInfo.SigilKind);

                case SyntaxTokenKind.StringLiteralToken:
                    return SyntaxToken.Literal(tokenInfo.Text, span, tokenInfo.StringValue);

                case SyntaxTokenKind.NumericLiteralToken:
                    return SyntaxToken.Literal(tokenInfo.Text, span, tokenInfo.DoubleValue);

                case SyntaxTokenKind.ParagraphStartTag:
                case SyntaxTokenKind.ParagraphEndTag:
                    return SyntaxToken.WithValue(tokenInfo.Kind, tokenInfo.Text, span, tokenInfo.StringValue);

                default:
                    return new SyntaxToken(tokenInfo.Kind, span);
            }
        }

        private SyntaxToken LexPXmlToken()
        {
            SyntaxTokenKind kind = SyntaxTokenKind.None;
            bool scanTrailingTrivia = false;
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '[':
                    kind = SyntaxTokenKind.ParagraphIdentifier;
                    ScanParagraphIdentifier();
                    scanTrailingTrivia = true;
                    break;

                case '\r':
                case '\n':
                    kind = SyntaxTokenKind.PXmlLineSeparator;
                    ScanEndOfLineSequence();
                    break;

                case EofCharacter:
                    kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    kind = SyntaxTokenKind.PXmlString;
                    ScanPXmlString();
                    break;
            }

            string text = GetCurrentLexeme();
            if (scanTrailingTrivia)
            {
                SkipSyntaxTrivia(isTrailing: true);
            }

            return SyntaxToken.WithValue(kind, text, GetCurrentLexemeSpan(), text);
        }

        private bool ScanIdentifier(ref TokenInfo tokenInfo)
        {
            bool isQuoted = false;
            if (PeekChar() == '"')
            {
                AdvanceChar();
                isQuoted = true;
            }

            switch (PeekChar())
            {
                case '@':
                    AdvanceChar();
                    // @->
                    if (PeekChar() == '-' && PeekChar(1) == '>')
                    {
                        AdvanceChar(2);
                        tokenInfo.SigilKind = SigilKind.Arrow;
                    }
                    break;

                case '$':
                    AdvanceChar();
                    tokenInfo.SigilKind = SigilKind.Dollar;
                    break;

                case '#':
                    AdvanceChar();
                    tokenInfo.SigilKind = SigilKind.Hash;
                    break;
            }

            int idxValueStart = CurrentLexemeLength;
            char c;
            while (SyntaxFacts.IsIdentifierPartCharacter((c = PeekChar())) && c != EofCharacter)
            {
                AdvanceChar();
            }

            int idxValueEnd = CurrentLexemeLength - idxValueStart;
            if (isQuoted)
            {
                EatChar('"');
            }

            var text = GetCurrentLexeme();
            if (text.Length == 0)
            {
                return false;
            }

            tokenInfo.Text = GetCurrentLexeme();
            tokenInfo.StringValue = tokenInfo.Text.Substring(idxValueStart, idxValueEnd);
            return true;
        }

        private void ScanBadToken()
        {
            while (!SyntaxFacts.IsWhitespace(PeekChar()) && PeekChar() != EofCharacter)
            {
                AdvanceChar();
            }
        }

        private void ScanStringLiteral(ref TokenInfo tokenInfo)
        {
            AdvanceChar();
            char c;
            while ((c = PeekChar()) != '"' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();

            tokenInfo.Text = GetCurrentLexeme();
            tokenInfo.StringValue = tokenInfo.Text.Substring(1, tokenInfo.Text.Length - 2);
        }

        private void ScanNumericLiteral(ref TokenInfo tokenInfo)
        {
            char character = PeekChar();
            bool isHex = character == '#';
            bool isPrefixedByAt = character == '@';
            if (!isHex)
            {
                if (isPrefixedByAt)
                {
                    AdvanceChar();
                    if ((character = PeekChar()) == '-' || character == '+')
                    {
                        AdvanceChar();
                    }
                }

                char c;
                while ((SyntaxFacts.IsDecDigit((c = PeekChar())) || c == '.') && c != EofCharacter)
                {
                    AdvanceChar();
                }
            }
            else
            {
                char c;
                while (SyntaxFacts.IsHexDigit((c = PeekChar())) && c != EofCharacter)
                {
                    AdvanceChar();
                }
            }

            string stringValue = tokenInfo.Text = GetCurrentLexeme();
            if (isHex)
            {
                stringValue = stringValue.Replace("#", "0x");
            }
            else if (isPrefixedByAt)
            {
                stringValue = stringValue.Substring(1);
            }

            var numberStyles = isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.AllowLeadingSign;
            numberStyles |= NumberStyles.AllowDecimalPoint;
            tokenInfo.DoubleValue = double.Parse(stringValue, numberStyles);
        }

        private bool IsIncludeDirective() => Match("#include");

        private bool IsHexNumericLiteral()
        {
            // In NSS, hex literals are always 3 bytes long.
            // So we need to check if the first 6 characters are valid hex digits
            // and then make sure those are followed by a stop character (whitespace/comma/etc).

            bool result = false;
            for (int n = 1; n < 7; n++)
            {
                char c = PeekChar(n);
                if (c == EofCharacter)
                {
                    return false;
                }

                if (!SyntaxFacts.IsHexDigit(c))
                {
                    result = false;
                }
            }

            if (!SyntaxFacts.IsIdentifierStopCharacter(PeekChar(7)))
            {
                result = false;
            }

            // TODO: expression is always false.
            return result;
        }

        private bool Is_PRE_StartTag() => Match("<pre");
        private bool Is_PRE_EndTag() => Match("</pre>");

        /// <summary>
        /// Returns true if the lookahead characters compose the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c;
                if ((c = PeekChar(i)) != s[i] && c != char.ToUpperInvariant(s[i]) || c == EofCharacter)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AdvanceIfMatches(string s)
        {
            if (Match(s))
            {
                AdvanceChar(s.Length);
                return true;
            }

            return false;
        }

        private void ScanPXmlString()
        {
            int preNestingLevel = 0;

            char c;
            while ((c = PeekChar()) != '{' && c != EofCharacter)
            {
                if (c == '<')
                {
                    if (Is_PRE_StartTag())
                    {
                        preNestingLevel++;
                        AdvanceChar(5);
                        continue;
                    }
                    else if (Is_PRE_EndTag())
                    {
                        if (preNestingLevel == 0)
                        {
                            break;
                        }

                        preNestingLevel--;
                        AdvanceChar(6);
                        continue;
                    }
                }

                int newlineSequenceLength = 0;
                while (SyntaxFacts.IsNewLine(PeekChar(newlineSequenceLength)))
                {
                    newlineSequenceLength++;
                    if (newlineSequenceLength >= 4)
                    {
                        return;
                    }
                }

                AdvanceChar();
            }
        }

        private void ScanParagraphStartTag(ref TokenInfo tokenInfo)
        {
            char c;
            while ((c = PeekChar()) != '>' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();
            tokenInfo.Text = GetCurrentLexeme();
            tokenInfo.StringValue = tokenInfo.Text.Substring(5, tokenInfo.Text.Length - 6);
        }

        private void ScanParagraphIdentifier()
        {
            AdvanceChar();

            char c;
            while ((c = PeekChar()) != ']' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();
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
                    if (isTrailing)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
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



                    case '>':
                        character = PeekChar(1);
                        if (SyntaxFacts.IsNewLine(character) || (character == '/' && PeekChar(2) == '/'))
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
            while (!((c = PeekChar()) == '*' && PeekChar(1) == '/') && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar(2);
        }
    }
}
