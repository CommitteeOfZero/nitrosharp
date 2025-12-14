using System;
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

    protected char PeekChar()
    {
        string text = Text;
        int index = Position;

        return (uint)index < (uint)text.Length
            ? text[index]
            : EofCharacter;
    }

    protected char PeekChar(int offset)
    {
        string text = Text;
        int index = Position + offset;

        return (uint)index < (uint)text.Length
            ? text[index]
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

    protected bool Match(string s, bool ignoreCase = false)
    {
        if (Position + s.Length > Text.Length) { return false; }

        ReadOnlySpan<char> actual = Text.AsSpan(Position, s.Length);
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return actual.Equals(s, comparison);
    }

    protected bool MatchAdvance(string s, bool ignoreCase = false)
    {
        if (Match(s, ignoreCase))
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
