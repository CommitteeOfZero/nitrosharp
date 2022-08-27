using System;
using System.Diagnostics;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript;

[DebuggerDisplay("{Value}")]
public readonly struct EntityQuery
{
    public EntityQuery(string query)
    {
        Value = query;
    }

    public readonly string Value;

    public EntityQueryPartEnumerable EnumerateParts() => new(Value.AsMemory());
}

public readonly struct EntityQueryPart : IEquatable<EntityQueryPart>
{
    public readonly ReadOnlyMemory<char> Value;
    public readonly int CharsConsumed;
    public readonly bool IsLast;

    public EntityQueryPart(ReadOnlyMemory<char> value, int nbConsumed, bool isLast)
    {
        Debug.Assert(value.Length > 0);
        Value = value;
        CharsConsumed = nbConsumed;
        IsLast = isLast;
    }

    public bool IsWildcardPattern => Value.Span[^1] == '*';
    public bool SearchInAliases => Value.Span[0] == '@';

    public bool Equals(EntityQueryPart other)
        => Value.Equals(other.Value) && IsLast == other.IsLast;
}

public struct EntityQueryEnumerator
{
    private EntityQueryPart _current;
    private ReadOnlyMemory<char> _remaining;
    private readonly int _queryLength;

    public EntityQueryEnumerator(ReadOnlyMemory<char> query)
    {
        _current = default;
        _remaining = query;
        _queryLength = query.Length;
    }

    public EntityQueryPart Current => _current;

    public bool MoveNext()
    {
        ReadOnlySpan<char> remaining = _remaining.Span;
        if (remaining.Length == 0) { return false; }
        int nextSeparator = remaining.IndexOf('/');
        (int curLen, int remStart, bool isLast) = nextSeparator >= 0
            ? (nextSeparator, nextSeparator + 1, false)
            : (remaining.Length, remaining.Length, true);
        int consumed = _queryLength - (_remaining.Length - remStart);
        _current = new EntityQueryPart(_remaining[..curLen], consumed, isLast);
        _remaining = _remaining[remStart..];
        return true;
    }
}

public readonly struct EntityQueryPartEnumerable
{
    private readonly ReadOnlyMemory<char> _query;

    public EntityQueryPartEnumerable(ReadOnlyMemory<char> query)
    {
        _query = query;
    }

    public SmallList<EntityQueryPart> ToSmallList()
    {
        var list = new SmallList<EntityQueryPart>();
        EntityQueryEnumerator enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            list.Add(enumerator.Current);
        }
        return list;
    }

    public EntityQueryEnumerator GetEnumerator() => new(_query);
}
