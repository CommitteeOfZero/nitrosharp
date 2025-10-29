using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScript;

public readonly record struct LinePosition(int Line, int Column);

public readonly record struct LinePositionSpan(LinePosition Start, LinePosition End);

public readonly record struct SourceLocation(SourceText SourceText, TextSpan Span)
{
    public LinePositionSpan GetLineSpan() => SourceText.GetLinePositionSpan(Span);
}

internal readonly record struct BytecodeSpan(int Start, int Length)
{
    public int End => Start + Length;
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SourceMapping(BytecodeSpan BytecodeSpan, TextSpan SourceSpan);

public readonly record struct TextSpan : IComparable<TextSpan>
{
    public TextSpan(int start, int length)
    {
        Debug.Assert(start >= 0 && length >= 0);
        Start = start;
        Length = length;
    }

    public int Start { get; }
    public int Length { get; }

    public int End => Start + Length;

    public override string ToString() => $"[{Start}..{End})";

    public static TextSpan FromBounds(int start, int end)
    {
        Debug.Assert(end >= start);
        return new TextSpan(start, end - start);
    }

    public int CompareTo(TextSpan other)
    {
        int diff = Start - other.Start;
        if (diff != 0)
        {
            return diff;
        }

        return Length - other.Length;
    }

    public void Deconstruct(out int start, out int end)
    {
        start = Start;
        end = End;
    }
}
