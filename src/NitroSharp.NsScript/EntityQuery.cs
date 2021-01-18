using System;
using System.Diagnostics;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript
{
    [DebuggerDisplay("{Value}")]
    public readonly struct EntityQuery
    {
        public EntityQuery(string query)
        {
            Value = query;
        }

        public readonly string Value;

        public EntityQueryPartEnumerable EnumerateParts()
            => new(Value.AsMemory());
    }

    public readonly struct EntityQueryPart : IEquatable<EntityQueryPart>
    {
        public readonly ReadOnlyMemory<char> Value;
        public readonly MouseState MouseState;
        public readonly int CharsConsumed;
        public readonly bool IsLast;

        public EntityQueryPart(ReadOnlyMemory<char> value, int nbConsumed, bool isLast)
        {
            Debug.Assert(value.Length > 0);
            Value = value;
            CharsConsumed = nbConsumed;
            ReadOnlySpan<char> span = value.Span;
            MouseState = span[0] == 'M'
                ? ParseMouseState(span)
                : MouseState.Invalid;
            IsLast = isLast;
        }

        public bool IsMouseState => MouseState != MouseState.Invalid;
        public bool IsPattern => !IsMouseState;

        public bool IsWildcardPattern
            => MouseState == MouseState.Invalid && Value.Span[^1] == '*';

        public bool SearchInAliases
            => MouseState == MouseState.Invalid && Value.Span[0] == '@';

        private static MouseState ParseMouseState(ReadOnlySpan<char> value)
        {
            if (value.Equals("MouseUsual", StringComparison.Ordinal))
            {
                return MouseState.Normal;
            }
            if (value.Equals("MouseOver", StringComparison.Ordinal))
            {
                return MouseState.Over;
            }
            if (value.Equals("MouseClick", StringComparison.Ordinal))
            {
                return MouseState.Down;
            }
            if (value.Equals("MouseLeave", StringComparison.Ordinal))
            {
                return MouseState.Leave;
            }

            return MouseState.Invalid;
        }

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
            => _query = query;

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

        public EntityQueryEnumerator GetEnumerator()
            => new(_query);
    }
}
