using System;
using System.IO;

namespace CommitteeOfZero.NsScript
{
    public abstract class TextScanner
    {
        // char.MaxValue is not a valid UTF-16 character, so it can safely be used as a EOF character.
        protected const char EofCharacter = char.MaxValue;

        // Position in the source text of where the current lexeme starts.
        protected int _lexemeStart;

        protected TextScanner()
        {
        }

        protected TextScanner(string sourceText)
        {
            SourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
        }

        protected string SourceText { get; set; }
        protected int Position { get; private set; }

        /// <summary>
        /// Gets the current lexeme, which is the characters between the LexemeStart marker and the current position.
        /// </summary>
        protected string CurrentLexeme
        {
            get
            {
                return CurrentLexemeLength != 0 ? SourceText.Substring(_lexemeStart, Position - _lexemeStart) : string.Empty;
            }
        }

        protected int CurrentLexemeLength => Position - _lexemeStart;

        protected char PeekChar() => PeekChar(0);
        protected char PeekChar(int offset)
        {
            if (Position + offset >= SourceText.Length)
            {
                return EofCharacter;
            }

            return SourceText[Position + offset];
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

        /// <summary>
        /// Marks the current position as the start of a lexeme.
        /// </summary>
        protected void StartScanning() => _lexemeStart = Position;

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
