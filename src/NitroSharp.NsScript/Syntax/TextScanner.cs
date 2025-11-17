using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Syntax;

internal abstract class TextScanner(string text)
{
    // char.MaxValue is not a valid UTF-16 character, so it can safely be used to indicate end of file.
    protected const char EofCharacter = char.MaxValue;

    protected string Text { get; } = text;
    protected int Position { get; private set; }
    protected int LexemeStart { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetPosition(int position)
    {
        Debug.Assert(position <= Text.Length && position >= 0);
        Position = position;
    }

    /// <summary>
    /// Marks the current position as the start of a lexeme.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void StartScanning()
    {
        LexemeStart = Position;
    }

    protected TextSpan CurrentLexemeSpan
        => new(start: LexemeStart, length: Position - LexemeStart);

    protected TextSpan CurrentSpanStart => new(CurrentLexemeSpan.Start, 0);

    protected char PeekChar() => PeekChar(0);

    protected char PeekChar(int offset)
    {
        return Position + offset < Text.Length
            ? Text[Position + offset]
            : EofCharacter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AdvanceChar()
    {
        Position++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AdvanceChar(int n)
    {
        Position += n;
    }

    protected void EatChar(char c)
    {
        char actualCharacter = PeekChar();
        if (actualCharacter != c)
        {
            Debug.Fail($"Error while scanning source text. Expected: '{c}', found: '{actualCharacter}'.");
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

    protected bool MatchInsensitive(string s)
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

    protected bool AdvanceIfMatchesInsensitive(string s)
    {
        if (MatchInsensitive(s))
        {
            AdvanceChar(s.Length);
            return true;
        }

        return false;
    }

    protected void ScanWhitespace()
    {
        char c;
        while (SyntaxFacts.IsWhitespace(c = PeekChar()) && c != EofCharacter)
        {
            AdvanceChar();
        }
    }

    protected void ScanToEndOfLine()
    {
        char c;
        while (!SyntaxFacts.IsNewLine(c = PeekChar()) && c != EofCharacter)
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

    protected int ScanEndOfLineSequence()
    {
        int len = 0;
        while (SyntaxFacts.IsNewLine(PeekChar()))
        {
            ScanEndOfLine();
            len++;
        }
        return len;
    }
}
