using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CommitteeOfZero.NsScript
{
    internal sealed class NsScriptLexer : TextScanner
    {
        private enum Location
        {
            Code,
            ParameterList,
            DialogueBlock
        }

        private static readonly Encoding s_defaultEncoding;
        private const string DialogueBlockEndTag = "</PRE>";

        static NsScriptLexer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s_defaultEncoding = Encoding.GetEncoding("shift-jis");
        }

        private Stack<Location> _nestingStack = new Stack<Location>();

        public NsScriptLexer(string sourceText) : base(sourceText)
        {
            _nestingStack.Push(Location.Code);
        }

        public NsScriptLexer(string fileName, Stream stream)
        {
            FileName = fileName;
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must support Read operation.");
            }

            using (var reader = new StreamReader(stream, s_defaultEncoding, true, 4096, leaveOpen: true))
            {
                SourceText = reader.ReadToEnd();
            }

            _nestingStack.Push(Location.Code);
        }

        public string FileName { get; }
        private Location CurrentLocation => _nestingStack.Peek();

        public SyntaxToken Lex()
        {
            if (CurrentLocation == Location.DialogueBlock)
            {
                if (PeekChar() != '{' && !IsDialogueBlockEndTag())
                {
                    return LexPXmlToken();
                }
            }

            var token = LexSyntaxToken();
            switch (token.Kind)
            {
                case SyntaxTokenKind.OpenBraceToken:
                    _nestingStack.Push(Location.Code);
                    break;

                case SyntaxTokenKind.CloseBraceToken:
                    _nestingStack.Pop();
                    break;

                case SyntaxTokenKind.FunctionKeyword:
                    _nestingStack.Push(Location.ParameterList);
                    break;

                case SyntaxTokenKind.CloseParenToken:
                    if (CurrentLocation == Location.ParameterList)
                    {
                        _nestingStack.Pop();
                    }
                    break;

                case SyntaxTokenKind.ParagraphStartTag:
                    _nestingStack.Push(Location.DialogueBlock);
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
                    if (CurrentLocation != Location.ParameterList && PeekChar(1) != '$')
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
                    if (nextChar == '=')
                    {
                        AdvanceChar(2);
                        kind = SyntaxTokenKind.LessThanEqualsToken;
                    }

                    // If the next character after '<' is a latin letter, it's most likely an XML tag.
                    // I've yet to find an exception to that.
                    else if (SyntaxFacts.IsLatinLetter(nextChar))
                    {
                        kind = SyntaxTokenKind.ParagraphStartTag;
                        ScanPXmlTag();
                    }
                    else if (nextChar == '/')
                    {
                        kind = SyntaxTokenKind.ParagraphEndTag;
                        ScanPXmlTag();
                    }
                    else
                    {
                        AdvanceChar();
                        kind = SyntaxTokenKind.LessThanToken;
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
                    if ((character = PeekChar()) == '=')
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
                    ScanDialogueBlockIdentifier();
                    scanTrailingTrivia = true;
                    break;

                case '\r':
                case '\n':
                    kind = SyntaxTokenKind.PXmlLineSeparator;
                    ScanEndOfLineSequence();
                    break;


                default:
                    kind = SyntaxTokenKind.PXmlString;
                    ScanPXmlNode();
                    break;
            }

            string text = CurrentLexeme;
            if (scanTrailingTrivia)
            {
                trailingTrivia = ScanSyntaxTrivia(isTrailing: true);
            }

            return new SyntaxToken(kind, string.Empty, text, trailingTrivia);
        }

        private void ScanPXmlNode()
        {
            char c;
            while (!IsDialogueBlockEndTag() && (c = PeekChar()) != '{' && c != EofCharacter)
            {
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

        private void ScanDialogueBlockIdentifier()
        {
            AdvanceChar();

            char c;
            while ((c = PeekChar()) != ']' && c != EofCharacter)
            {
                AdvanceChar();
            }

            AdvanceChar();
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
                    if ((character = PeekChar()) == '-' && PeekChar(1) == '>')
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

            return result;
        }

        private bool IsDialogueBlockEndTag()
        {
            for (int i = 0; i < DialogueBlockEndTag.Length; i++)
            {
                if (PeekChar(i) != DialogueBlockEndTag[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ScanPXmlTag()
        {
            char c;
            while ((c = PeekChar()) != '>' && c != EofCharacter)
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
