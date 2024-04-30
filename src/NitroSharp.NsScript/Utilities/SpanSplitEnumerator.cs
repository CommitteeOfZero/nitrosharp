using System;

namespace NitroSharp.NsScript.Utilities;

public ref struct SpanSplitEnumerator
{
    private ReadOnlySpan<char> _remaining;
    private readonly char _separator;

    public SpanSplitEnumerator(ReadOnlySpan<char> text, char separator)
    {
        Current = default;
        _remaining = text;
        _separator = separator;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    public bool MoveNext()
    {
        ReadOnlySpan<char> remaining = _remaining;
        if (remaining == default) { return false; }

        int nextSeparator = remaining.IndexOf(_separator);
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
