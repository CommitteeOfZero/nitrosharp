using NitroSharp.NsScriptNew.Text;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew.Syntax
{
    internal abstract class TextScanner
    {
        // char.MaxValue is not a valid UTF-16 character, so it can safely be used to indicate end of file.
        protected const char EofCharacter = char.MaxValue;

        private readonly string _text;
        private int _position;
        private int _lexemeStart;

        protected TextScanner(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        protected int Position => _position;
        protected int LexemeStart => _lexemeStart;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetPosition(int position)
        {
            Debug.Assert(position <= _text.Length && position >= 0);
            _position = position;
        }

        /// <summary>
        /// Marks the current position as the start of a lexeme.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void StartScanning() => _lexemeStart = _position;

        protected TextSpan CurrentLexemeSpan =>
            new TextSpan(start: _lexemeStart, length: _position - _lexemeStart);

        protected TextSpan CurrentSpanStart => new TextSpan(CurrentLexemeSpan.Start, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char PeekChar() => PeekChar(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected char PeekChar(int offset)
        {
            if (_position + offset >= _text.Length)
            {
                return EofCharacter;
            }

            return _text[_position + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AdvanceChar() => _position++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AdvanceChar(int n) => _position += n;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EatChar(char c)
        {
            char actualCharacter = PeekChar();
            if (actualCharacter != c)
            {
                Debug.Fail($"Error while scanning source text. Expected: '{c}', found: '{actualCharacter}'.");
            }

            AdvanceChar();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
