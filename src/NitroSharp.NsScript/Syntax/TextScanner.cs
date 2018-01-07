using NitroSharp.NsScript.Text;
using System;
using System.IO;

namespace NitroSharp.NsScript.Syntax
{
    internal abstract class TextScanner
    {
        // char.MaxValue is not a valid UTF-16 character, so it can safely be used as a EOF character.
        protected const char EofCharacter = char.MaxValue;

        private readonly string _text;
        private int _position;

        // Position in the source text of where the current lexeme starts.
        protected int _lexemeStart;

        protected TextScanner(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Gets the current lexeme, which is the characters between the LexemeStart marker and the current position.
        /// </summary>
        protected string GetCurrentLexeme()
        {
            return CurrentLexemeLength > 0 ? _text.Substring(_lexemeStart, _position - _lexemeStart) : string.Empty;
        }

        protected TextSpan GetCurrentLexemeSpan()
        {
            return new TextSpan(_lexemeStart, _position - _lexemeStart);
        }

        protected int CurrentLexemeLength => _position - _lexemeStart;

        protected char PeekChar() => PeekChar(0);
        protected char PeekChar(int offset)
        {
            if (_position + offset >= _text.Length)
            {
                return EofCharacter;
            }

            return _text[_position + offset];
        }

        protected void AdvanceChar() => _position++;
        protected void AdvanceChar(int n) => _position += n;

        protected void EatChar(char c)
        {
            char actualCharacter = PeekChar();
            if (actualCharacter != c)
            {
                throw new InvalidDataException();
            }

            AdvanceChar();
        }

        /// <summary>
        /// Marks the current position as the start of a lexeme.
        /// </summary>
        protected void StartScanning() => _lexemeStart = _position;

        protected void ScanWhitespace()
        {
            char c;
            while (SyntaxFacts.IsWhitespace((c = PeekChar())) && c != EofCharacter)
            {
                AdvanceChar();
            }
        }

        protected void ScanToEndOfLine()
        {
            char c;
            while (!SyntaxFacts.IsNewLine((c = PeekChar())) && c != EofCharacter)
            {
                AdvanceChar();
            }
        }

        protected void ScanEndOfLine()
        {
            char c = PeekChar();
            switch (c)
            {
                case '\r':
                    AdvanceChar();
                    if (PeekChar() == '\n')
                    {
                        AdvanceChar();
                    }
                    break;

                case '\n':
                    AdvanceChar();
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(c))
                    {
                        AdvanceChar();
                    }
                    break;
            }
        }

        protected void ScanEndOfLineSequence()
        {
            while (SyntaxFacts.IsNewLine(PeekChar()))
            {
                ScanEndOfLine();
            }
        }
    }
}
