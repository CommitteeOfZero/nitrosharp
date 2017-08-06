using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript
{
    public sealed class NsScriptLexer : TextScanner
    {
        public enum Context
        {
            Code,
            ParameterList,
            Paragraph
        }

        private const string PRE_StartTag = "<pre>";
        private const string PRE_EndTag = "</pre>";

        private readonly Context _initialContext;
        private readonly Stack<Context> _nestingStack = new Stack<Context>();

        public NsScriptLexer(string sourceText, Context initialContext = Context.Code) : base(sourceText)
        {
            _initialContext = initialContext;
            _nestingStack.Push(initialContext);
        }

        public NsScriptLexer(string sourceText, string fileName, Context initialContext = Context.Code)
            : this(sourceText, initialContext)
        {
            FileName = fileName;
        }

        public string FileName { get; }
        private Context CurrentContext
        {
            get => PeekChar() != EofCharacter ? _nestingStack.Peek() : _initialContext;
        }

        public SyntaxToken Lex()
        {
            if (CurrentContext == Context.Paragraph)
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
                    _nestingStack.Push(Context.Code);
                    break;

                case SyntaxTokenKind.CloseBraceToken:
                    _nestingStack.Pop();
                    break;

                case SyntaxTokenKind.FunctionKeyword:
                    _nestingStack.Push(Context.ParameterList);
                    break;

                case SyntaxTokenKind.CloseParenToken:
                    if (CurrentContext == Context.ParameterList)
                    {
                        _nestingStack.Pop();
                    }
                    break;

                case SyntaxTokenKind.ParagraphStartTag:
                    _nestingStack.Push(Context.Paragraph);
                    break;

                case SyntaxTokenKind.ParagraphEndTag:
                    _nestingStack.Pop();
                    break;
            }

            return token;
        }

        private SyntaxToken LexSyntaxToken()
        {
            string leadingTrivia = ScanSyntaxTrivia(isTrailing: false);

            SyntaxTokenKind kind = SyntaxTokenKind.None;
            string text = null;
            object value = null;
            StartScanning();

            char character = PeekChar();
            switch (character)
            {
                case '"':
                    if (CurrentContext != Context.ParameterList && PeekChar(1) != '$')
                    {
                        ScanStringLiteral();
                        kind = SyntaxTokenKind.StringLiteralToken;
                    }
                    else
                    {
                        ScanIdentifier();
                        kind = SyntaxTokenKind.IdentifierToken;
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
                    value = ScanNumericLiteral();
                    kind = SyntaxTokenKind.NumericLiteralToken;
                    break;

                case '$':
                    ScanIdentifier();
                    kind = SyntaxTokenKind.IdentifierToken;
                    break;

                case '#':
                    if (IsIncludeDirective())
                    {
                        ScanToEndOfLine();
                        kind = SyntaxTokenKind.IncludeDirective;
                    }
                    else if (IsHexNumericLiteral())
                    {
                        value = ScanNumericLiteral();
                        kind = SyntaxTokenKind.NumericLiteralToken;
                    }
                    else
                    {
                        ScanIdentifier();
                        kind = SyntaxTokenKind.IdentifierToken;
                    }
                    break;

                case '@':
                    char next = PeekChar(1);
                    if (PeekChar(1) == '-' && PeekChar(2) == '>')
                    {
                        ScanIdentifier();
                        kind = SyntaxTokenKind.IdentifierToken;
                    }
                    else if (next == '+' || next == '-' || SyntaxFacts.IsDecDigit(next))
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.AtToken;
                    }
                    else
                    {
                        ScanIdentifier();
                        kind = SyntaxTokenKind.IdentifierToken;
                    }
                    break;

                case '<':
                    char nextChar = PeekChar(1);
                    switch (nextChar)
                    {
                        case '=':
                            AdvanceChar(2);
                            kind = SyntaxTokenKind.LessThanEqualsToken;
                            break;

                        case 'p':
                        case 'P':
                            ScanParagraphStartTag();
                            kind = SyntaxTokenKind.ParagraphStartTag;
                            break;

                        case '/':
                            AdvanceChar(PRE_EndTag.Length);
                            kind = SyntaxTokenKind.ParagraphEndTag;
                            break;

                        default:
                            AdvanceChar();
                            kind = SyntaxTokenKind.LessThanToken;
                            break;
                    }
                    break;

                case '{':
                    AdvanceChar();
                    kind = SyntaxTokenKind.OpenBraceToken;
                    break;

                case '}':
                    AdvanceChar();
                    kind = SyntaxTokenKind.CloseBraceToken;
                    break;

                case '(':
                    AdvanceChar();
                    kind = SyntaxTokenKind.OpenParenToken;
                    break;

                case ')':
                    AdvanceChar();
                    kind = SyntaxTokenKind.CloseParenToken;
                    break;

                case '.':
                    AdvanceChar();
                    kind = SyntaxTokenKind.DotToken;
                    break;

                case ',':
                    AdvanceChar();
                    kind = SyntaxTokenKind.CommaToken;
                    break;

                case ':':
                    AdvanceChar();
                    kind = SyntaxTokenKind.ColonToken;
                    break;

                case ';':
                    AdvanceChar();
                    kind = SyntaxTokenKind.SemicolonToken;
                    break;

                case '=':
                    AdvanceChar();
                    if ((PeekChar()) == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.EqualsEqualsToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.EqualsToken;
                    }
                    break;

                case '+':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.PlusEqualsToken;
                    }
                    else if (character == '+')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.PlusPlusToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.PlusToken;
                    }
                    break;

                case '-':
                    AdvanceChar();
                    if ((character = PeekChar()) == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.MinusEqualsToken;
                    }
                    else if (character == '-')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.MinusMinusToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.MinusToken;
                    }
                    break;

                case '*':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.AsteriskEqualsToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.AsteriskToken;
                    }
                    break;

                case '/':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.SlashEqualsToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.SlashToken;
                    }
                    break;

                case '>':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.GreaterThanEqualsToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.GreaterThanToken;
                    }
                    break;

                case '!':
                    AdvanceChar();
                    if (PeekChar() == '=')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.ExclamationEqualsToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.ExclamationToken;
                    }
                    break;

                case '|':
                    AdvanceChar();
                    if (PeekChar() == '|')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.BarBarToken;
                    }
                    break;

                case '&':
                    AdvanceChar();
                    if (PeekChar() == '&')
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.AmpersandAmpersandToken;
                    }
                    else
                    {
                        kind = SyntaxTokenKind.AmpersandToken;
                    }
                    break;

                case EofCharacter:
                    kind = SyntaxTokenKind.EndOfFileToken;
                    break;

                default:
                    ScanIdentifier();
                    string identifier = CurrentLexeme;
                    kind = SyntaxFacts.GetKeywordKind(identifier);
                    switch (kind)
                    {
                        case SyntaxTokenKind.None:
                            kind = SyntaxTokenKind.IdentifierToken;
                            break;

                        case SyntaxTokenKind.NullKeyword:
                            value = null;
                            break;

                        case SyntaxTokenKind.TrueKeyword:
                            value = true;
                            break;

                        case SyntaxTokenKind.FalseKeyword:
                            value = false;
                            break;
                    }
                    break;
            }

            text = CurrentLexeme;
            if (kind == SyntaxTokenKind.StringLiteralToken)
            {
                value = text.Substring(1, text.Length - 2);
            }

            string trailingTrivia = ScanSyntaxTrivia(isTrailing: true);
            return new SyntaxToken(kind, leadingTrivia, text, trailingTrivia, value);
        }

        private SyntaxToken LexPXmlToken()
        {
            SyntaxTokenKind kind = SyntaxTokenKind.None;
            string trailingTrivia = string.Empty;
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

            string text = CurrentLexeme;
            if (scanTrailingTrivia)
            {
                trailingTrivia = ScanSyntaxTrivia(isTrailing: true);
            }

            return new SyntaxToken(kind, string.Empty, text, trailingTrivia);
        }

        private void ScanIdentifier()
        {
            char character = PeekChar();
            bool startsWithQuote = false;
            switch (character)
            {
                case '@':
                    AdvanceChar();
                    // @->
                    if (PeekChar() == '-' && PeekChar(1) == '>')
                    {
                        AdvanceChar(2);
                    }
                    break;

                case '$':
                case '#':
                    AdvanceChar();
                    break;

                case '"':
                    AdvanceChar();
                    startsWithQuote = true;
                    break;
            }

            char c;
            while (SyntaxFacts.IsIdentifierPartCharacter((c = PeekChar())) && c != EofCharacter)
            {
                AdvanceChar();
            }

            if (startsWithQuote)
            {
                AdvanceChar();
            }
        }

        private void ScanStringLiteral()
        {
            AdvanceChar();
            char c;
            while ((c = PeekChar()) != '"' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();
        }

        private int ScanNumericLiteral()
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
                while (SyntaxFacts.IsDecDigit((c = PeekChar())) && c != EofCharacter)
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

            string strValue = CurrentLexeme;
            if (isHex)
            {
                strValue = strValue.Replace("#", "0x");
            }
            else if (isPrefixedByAt)
            {
                strValue = strValue.Substring(1);
            }

            var numberStyles = isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.AllowLeadingSign;
            return int.Parse(strValue, numberStyles);
        }

        private bool IsIncludeDirective()
        {
            string include = "#include";
            for (int i = 0; i < include.Length; i++)
            {
                if (PeekChar(i) != include[i])
                {
                    return false;
                }
            }

            return true;
        }

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

        private void ScanParagraphStartTag()
        {
            char c;
            while ((c = PeekChar()) != '>' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();
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

        private string ScanSyntaxTrivia(bool isTrailing)
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

                    // Lines starting with '.' are pretty common.
                    // Treat them as single line comments.
                    case '.':
                        ScanToEndOfLine();
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

            return CurrentLexeme;
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
