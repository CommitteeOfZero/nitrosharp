using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NitroSharp.NsScript;

public readonly record struct SourceLocation(int Line, int Column, int Length);

internal readonly record struct BytecodeLocation(int Start, int Length)
{
    public int End => Start + Length;
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SourceMapping(BytecodeLocation BytecodeLocation, SourceLocation SourceLocation);

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
}

