using System;
using System.IO;

namespace SciAdvNet.NSScript
{
    internal class TextScanner
    {
        // char.MaxValue is not a valid UTF-16 character, so it can safely be used as a EOF character.
        public const char EofCharacter = char.MaxValue;

        // A marker that we set at the beginning of each lexeme.
        protected int _lexemeStart;
        //private bool _scanningFunctionSignature;

        //// Indicates whether we're inside a dialogue block, a.k.a <PRE> element.
        //private bool _isInsideDialogueBlock;
        //// Number of unmatched braces inside a dialogue block.
        //// When it's equal to 0, the lexer mode is switched back from Normal to PXmlSyntax.
        //private uint _dlgUnmatchedBraces = 0;

        public TextScanner()
        {
        }

        public TextScanner(string sourceText)
        {
            SourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
        }

        public string SourceText { get; protected set; }
        public int Position { get; private set; }

        /// <summary>
        /// Gets the current lexeme, which is the characters between the lexemeStart marker and the current position.
        /// </summary>
        public string CurrentLexeme
        {
            get
            {
                return CurrentLexemeLength != 0 ? SourceText.Substring(_lexemeStart, Position - _lexemeStart) : string.Empty;
            }
        }

        public int CurrentLexemeLength => Position - _lexemeStart;

        public char PeekChar() => PeekChar(0);
        public char PeekChar(int offset)
        {
            if (Position + offset >= SourceText.Length)
            {
                return EofCharacter;
            }

            return SourceText[Position + offset];
        }

        public void AdvanceChar() => Position++;
        public void AdvanceChar(int n) => Position += n;

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
        /// Sets the LexemeStart marker at the current position.
        /// </summary>
        public void StartScanning() => _lexemeStart = Position;
    }
}
