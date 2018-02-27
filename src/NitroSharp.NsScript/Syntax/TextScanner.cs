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

        private readonly string _text;

        protected TextScanner(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        protected int Position { get; private set; }
        protected int LexemeStart { get; private set; }

        protected void SetPosition(int position)
        {
            if (position >= _text.Length || position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            Position = position;
        }

        /// <summary>
        /// Marks the current position as the start of a lexeme.
        /// </summary>
        protected void StartScanning() => LexemeStart = Position;

        /// <summary>
        /// Gets the current lexeme, which is the characters between the LexemeStart marker and the current position.
        /// </summary>
        protected string GetCurrentLexeme()
        {
            return CurrentLexemeLength > 0 ? _text.Substring(LexemeStart, Position - LexemeStart) : string.Empty;
        }

        protected TextSpan CurrentLexemeSpan => new TextSpan(LexemeStart, Position - LexemeStart);
        protected TextSpan CurrentSpanStart => new TextSpan(CurrentLexemeSpan.Start, 0);

        private int CurrentLexemeLength => Position - LexemeStart;

        protected char PeekChar() => PeekChar(0);
        protected char PeekChar(int offset)
        {
            if (Position + offset >= _text.Length)
            {
                return EofCharacter;
            }

            return _text[Position + offset];
        }

        protected void AdvanceChar() => Position++;
        protected void AdvanceChar(int n) => Position += n;

        protected void EatChar(char c)
        {
            char actualCharacter = PeekChar();
            if (actualCharacter != c)
            {
                throw new InvalidDataException();
            }

            AdvanceChar();
        }

        protected bool TryEatChar(char c)
        {
            char actualCharacter = PeekChar();
            if (actualCharacter != c)
            {
                return false;
            }

            AdvanceChar();
            return true;
        }

        /// <summary>
        /// Returns true if the lookahead characters compose the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Match(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c;
                if ((c = PeekChar(i)) != s[i] && c != char.ToUpperInvariant(s[i]))
                {
                    return false;
                }
            }

            return true;
        }

        protected bool AdvanceIfMatches(string s)
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
