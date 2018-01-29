using NitroSharp.NsScript.Text;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Syntax
{
    internal abstract class TextScanner
    {
        // char.MaxValue is not a valid UTF-16 character, so it can safely be used to indicate end of file.
        protected const char EofCharacter = char.MaxValue;

        private string _text;
        private int _position;

        // Absolute position in the source text of where the current lexeme starts.
        protected int _lexemeStart;

        protected TextScanner() : this(string.Empty) { }
        protected TextScanner(string text)
        {
            Reset(text);
        }

        protected void Reset(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _position = 0;
            _lexemeStart = 0;
        }

        /// <summary>
        /// Marks the current position as the start of a lexeme.
        /// </summary>
        public void StartScanning() => _lexemeStart = _position;

        /// <summary>
        /// Gets the current lexeme, which is the characters between the LexemeStart marker and the current position.
        /// </summary>
        public string GetCurrentLexeme()
        {
            return CurrentLexemeLength > 0 ? _text.Substring(_lexemeStart, _position - _lexemeStart) : string.Empty;
        }

        public TextSpan GetCurrentLexemeSpan()
        {
            return new TextSpan(_lexemeStart, _position - _lexemeStart);
        }

        public int CurrentLexemeLength => _position - _lexemeStart;

        public char PeekChar() => PeekChar(0);
        public char PeekChar(int offset)
        {
            if (_position + offset >= _text.Length)
            {
                return EofCharacter;
            }

            return _text[_position + offset];
        }

        public void AdvanceChar() => _position++;
        public void AdvanceChar(int n) => _position += n;

        public void EatChar(char c)
        {
            char actualCharacter = PeekChar();
            if (actualCharacter != c)
            {
                throw new InvalidDataException();
            }

            AdvanceChar();
        }

        /// <summary>
        /// Returns true if the lookahead characters compose the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(string s)
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

        public bool AdvanceIfMatches(string s)
        {
            if (Match(s))
            {
                AdvanceChar(s.Length);
                return true;
            }

            return false;
        }

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
