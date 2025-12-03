using System;

namespace NitroSharp.Common;

public ref struct SpanSplitEnumerator(ReadOnlySpan<char> text, char separator)
{
    private ReadOnlySpan<char> _remaining = text;

    public ReadOnlySpan<char> Current { get; private set; } = default;

    public bool MoveNext()
    {
        ReadOnlySpan<char> remaining = _remaining;
        if (remaining.IsEmpty) { return false; }

        int nextSeparator = remaining.IndexOf(separator);
        (int currentLength, int remainingStart) = nextSeparator >= 0
            ? (nextSeparator, nextSeparator + 1)
            : (remaining.Length, remaining.Length);
        Current = _remaining[..currentLength];
        _remaining = nextSeparator >=0
            ? _remaining[remainingStart..]
            : default;
        return true;
    }

    public SpanSplitEnumerator GetEnumerator() => this;
}

public static class ReadOnlySpanExtensions
{
    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> s, char separator) => new(s, separator);
}
