using System.Globalization;
using System.Collections.Generic;
using NitroSharp.NsScript.Text;
using System.Runtime.CompilerServices;

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
            public bool IsQuotedIdentifier;
            public bool IsHexTriplet;
            public TextSpan Span;
        }

        private const string PRE_StartTag = "<pre>";
        private const string PRE_EndTag = "</pre>";

        private readonly LexingMode _initialMode;
        private readonly Stack<LexingMode> _lexingModeStack = new Stack<LexingMode>();
        private Diagnostic _lastSyntaxError;

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
            if (CurrentMode == LexingMode.DialogueBlock)
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
                    if (PeekChar(1) != '$')
                    {
                        ScanStringLiteral(ref info);
                        // Could actually be a quoted keyword (e.g "null")
                        if (SyntaxFacts.TryGetKeywordKind(info.StringValue, out var keywordKind))
                        {
                            switch (keywordKind)
                            {
                                case SyntaxTokenKind.NullKeyword:
                                case SyntaxTokenKind.TrueKeyword:
                                case SyntaxTokenKind.FalseKeyword:
                                    info.Kind = keywordKind;
                                    break;
                            }

                        }
                    }
                    else // it's a quoted identifier
                    {
                        ScanIdentifier(ref info);
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
                    if (!ScanDecNumericLiteral(ref info))
                    {
                        // If it's not a number, then it's an identifier starting with a number.
                        goto default;
                    }
                    break;

                case '$':
                    if (!ScanIdentifier(ref info))
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.DollarToken;
                    }
                    break;

                case '#':
                    if (AdvanceIfMatches("#include"))
                    {
                        info.Kind = SyntaxTokenKind.IncludeDirective;
                    }
                    else if (!ScanHexTriplet(ref info) && !ScanIdentifier(ref info))
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.HashToken;
                    }
                    break;

                case '@':
                    char next = PeekChar(1);
                    if (PeekChar(1) == '-' && PeekChar(2) == '>')
                    {
                        AdvanceChar(3);
                        info.Kind = SyntaxTokenKind.AtArrowToken;
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
                            if (!ScanDialogueBlockStartTag(ref info))
                            {
                                goto default;
                            }
                            break;

                        case '/':
                            if (AdvanceIfMatches(PRE_EndTag))
                            {
                                info.Kind = SyntaxTokenKind.DialogueBlockEndTag;
                            }
                            else
                            {
                                goto default;
                            }
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
                    else if (character == '>')
                    {
                        AdvanceChar();
                        info.Kind = SyntaxTokenKind.ArrowToken;
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
                        if (SyntaxFacts.TryGetKeywordKind(info.StringValue, out var keywordKind))
                        {
                            info.Kind = keywordKind;
                        }
                    }
                    else
                    {
                        ScanBadToken(ref info);
                    }
                    break;
            }

            var token = CreateToken(ref info);
            SkipSyntaxTrivia(isTrailing: true);
            return token;
        }

        private SyntaxToken CreateToken(ref TokenInfo tokenInfo)
        {
            var span = tokenInfo.Span;
            if (span == default(TextSpan))
            {
                span = CurrentLexemeSpan;
            }

            var error = _lastSyntaxError;
            _lastSyntaxError = null;

            switch (tokenInfo.Kind)
            {
                case SyntaxTokenKind.IdentifierToken:
                    return SyntaxToken.Identifier(tokenInfo.StringValue, span, tokenInfo.SigilKind, tokenInfo.IsQuotedIdentifier, error);

                case SyntaxTokenKind.StringLiteralToken:
                    return SyntaxToken.Literal(tokenInfo.StringValue, span, error);

                case SyntaxTokenKind.NumericLiteralToken:
                    return tokenInfo.IsHexTriplet
                        ? SyntaxToken.HexTriplet(tokenInfo.DoubleValue, span, error)
                        : SyntaxToken.Literal(tokenInfo.DoubleValue, span, error);

                case SyntaxTokenKind.DialogueBlockStartTag:
                    return SyntaxToken.DialogueBlockStartTag(tokenInfo.StringValue, span, error);

                case SyntaxTokenKind.DialogueBlockIdentifier:
                    return SyntaxToken.DialogueBlockIdentifier(tokenInfo.StringValue, span, error);

                case SyntaxTokenKind.PXmlString:
                    return SyntaxToken.WithText(SyntaxTokenKind.PXmlString, tokenInfo.Text, span, error);

                case SyntaxTokenKind.BadToken:
                    return SyntaxToken.WithText(SyntaxTokenKind.BadToken, tokenInfo.Text, span, error);

                default:
                    return new SyntaxToken(tokenInfo.Kind, span, error);
            }
        }

        private SyntaxToken LexPXmlToken()
        {
            var info = new TokenInfo();
            bool skipTrailingTrivia = false;
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '[':
                    ScanDialogueBlockIdentifier(ref info);
                    skipTrailingTrivia = true;
                    break;

                case '\r':
                case '\n':
                    info.Kind = SyntaxTokenKind.PXmlLineSeparator;
                    ScanEndOfLineSequence();
                    break;

                case EofCharacter:
                    info.Kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    ScanPXmlString(ref info);
                    break;
            }

            if (skipTrailingTrivia)
            {
                SkipSyntaxTrivia(isTrailing: true);
            }

            return CreateToken(ref info);
        }

        private bool ScanIdentifier(ref TokenInfo tokenInfo)
        {
            int start = Position;
            bool isQuoted = false;
            if (PeekChar() == '"')
            {
                AdvanceChar();
                isQuoted = true;
            }

            switch (PeekChar())
            {
                case '$':
                    AdvanceChar();
                    tokenInfo.SigilKind = SigilKind.Dollar;
                    break;

                case '#':
                    AdvanceChar();
                    tokenInfo.SigilKind = SigilKind.Hash;
                    break;
            }

            StartScanning();
            while (SyntaxFacts.IsIdentifierPartCharacter(PeekChar()))
            {
                AdvanceChar();
            }

            string value = GetCurrentLexeme();
            if (isQuoted && !TryEatChar('"'))
            {
                Report(DiagnosticId.UnterminatedQuotedIdentifier, new TextSpan(start, 0));
            }

            int end = Position;
            bool empty = value.Length == 0;
            if (empty)
            {
                SetPosition(start);
                return false;
            }

            tokenInfo.Kind = SyntaxTokenKind.IdentifierToken;
            tokenInfo.StringValue = value;
            tokenInfo.IsQuotedIdentifier = isQuoted;
            tokenInfo.Span = new TextSpan(start, end - start);
            return true;
        }

        private void ScanStringLiteral(ref TokenInfo tokenInfo)
        {
            int start = Position;
            EatChar('"');
            StartScanning();

            char c;
            while ((c = PeekChar()) != '"' && c != EofCharacter)
            {
                AdvanceChar();
            }

            tokenInfo.StringValue = GetCurrentLexeme(); // value without the quotes
            if (!TryEatChar('"'))
            {
                Report(DiagnosticId.UnterminatedString, new TextSpan(start, 0));
            }

            int end = Position;
            tokenInfo.Span = new TextSpan(start, end - start);
            tokenInfo.Kind = SyntaxTokenKind.StringLiteralToken;
        }

        private bool ScanDecNumericLiteral(ref TokenInfo tokenInfo)
        {
            char c;
            while ((SyntaxFacts.IsDecDigit((c = PeekChar())) || c == '.'))
            {
                AdvanceChar();
            }

            string stringValue = tokenInfo.Text = GetCurrentLexeme();
            bool valid = double.TryParse(stringValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tokenInfo.DoubleValue);
            if (!valid)
            {
                Report(DiagnosticId.NumberTooLarge);
            }

            // If the next character is a valid identifier character,
            // then what we're scanning is actually an identifier that starts with a number
            // e.g "215_ＡＡルートグッドエンド".
            if (SyntaxFacts.IsIdentifierPartCharacter(PeekChar()))
            {
                SetPosition(LexemeStart);
                return false;
            }

            tokenInfo.Kind = SyntaxTokenKind.NumericLiteralToken;
            return true;
        }

        private bool ScanHexTriplet(ref TokenInfo tokenInfo)
        {
            int start = Position;
            EatChar('#');
            StartScanning();

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
            if (SyntaxFacts.IsIdentifierPartCharacter(PeekChar()))
            {
                SetPosition(start);
                return false;
            }

            int end = Position;
            string stringValue = tokenInfo.Text = GetCurrentLexeme(); // value without the '#'
            // Not expected to throw
            tokenInfo.DoubleValue = int.Parse(stringValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            tokenInfo.IsHexTriplet = true;
            tokenInfo.Span = new TextSpan(start, end - start);
            tokenInfo.Kind = SyntaxTokenKind.NumericLiteralToken;
            return true;
        }

        private bool Is_PRE_EndTag() => Match("</pre>");

        private void ScanPXmlString(ref TokenInfo tokenInfo)
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
                    else if (Match(PRE_EndTag))
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

                int newlineSequenceLength = 0;
                while (SyntaxFacts.IsNewLine(PeekChar(newlineSequenceLength)))
                {
                    newlineSequenceLength++;
                    if (newlineSequenceLength >= 4)
                    {
                        goto exit;
                    }
                }

                AdvanceChar();
            }

            exit:
            tokenInfo.Kind = SyntaxTokenKind.PXmlString;
            tokenInfo.Text = GetCurrentLexeme();
        }

        private bool ScanDialogueBlockStartTag(ref TokenInfo tokenInfo)
        {
            int start = Position;
            if (!AdvanceIfMatches("<PRE "))
            {
                return false;
            }

            StartScanning();

            char c;
            while ((c = PeekChar()) != '>' && !IsEofOrNewLine(c))
            {
                AdvanceChar();
            }

            tokenInfo.StringValue = GetCurrentLexeme(); // just the box name, without the '<PRE  >'
            if (!TryEatChar('>'))
            {
                Report(DiagnosticId.UnterminatedDialogueBlockStartTag, new TextSpan(start, 0));
            }

            int end = Position;
            tokenInfo.Span = new TextSpan(start, end - start);
            tokenInfo.Kind = SyntaxTokenKind.DialogueBlockStartTag;
            return true;
        }

        private void ScanDialogueBlockIdentifier(ref TokenInfo tokenInfo)
        {
            int start = Position;
            AdvanceChar();
            StartScanning();

            char c;
            while ((c = PeekChar()) != ']' && !IsEofOrNewLine(c))
            {
                AdvanceChar();
            }

            tokenInfo.StringValue = GetCurrentLexeme();
            if (!TryEatChar(']'))
            {
                Report(DiagnosticId.UnterminatedDialogueBlockIdentifier, new TextSpan(start, 0));
            }

            int end = Position;
            tokenInfo.Span = new TextSpan(start, end - start);
            tokenInfo.Kind = SyntaxTokenKind.DialogueBlockIdentifier;
        }

        private void ScanBadToken(ref TokenInfo tokenInfo)
        {
            while (!IsEofOrNewLine(PeekChar()))
            {
                AdvanceChar();
            }

            tokenInfo.Text = GetCurrentLexeme();
            tokenInfo.Kind = SyntaxTokenKind.BadToken;
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

                    case '.':
                    case '>':
                        // The following character sequences are treated as "//":
                        // ".//"
                        // ">//"
                        // ".."
                        if (PeekChar(1) == '/' && PeekChar(2) == '/' || character == '.' && PeekChar(1) == '.')
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEofOrNewLine(char c)
        {
            switch (c)
            {
                case EofCharacter:
                case '\r':
                case '\n':
                    return true;

                default:
                    return false;
            }
        }

        private void Report(DiagnosticId diagnosticId) => Report(diagnosticId, CurrentLexemeSpan);
        private void Report(DiagnosticId diagnosticId, TextSpan textSpan)
        {
            _lastSyntaxError = Diagnostic.Create(textSpan, diagnosticId);
        }
    }
}
