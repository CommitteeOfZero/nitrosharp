using System;
using System.Runtime.CompilerServices;
using NitroSharp.Interactivity;

#nullable enable

namespace NitroSharp
{
    internal readonly struct EntityName : IEquatable<EntityName>
    {
        private readonly string _value;
        private readonly string? _parent;
        private readonly ShortSpan _name;

        public EntityName(string value)
        {
            _value = value;
            _name = ShortSpan.Empty;
            _parent = null;
            MouseState = null;

            var parts = new PartEnumerable(value, separator: '/');
            ushort parentStart = ushort.MaxValue, parentLength = 0;
            foreach (ShortSpan part in parts)
            {
                if (TryParseMouseState(part, out MouseState mouseState))
                {
                    MouseState = mouseState;
                    continue;
                }
                if ((part.Start + part.Length) == _value.Length)
                {
                    _name = part;
                }
                else
                {
                    parentStart = Math.Min(parentStart, part.Start);
                    parentLength = (ushort)(part.Start + part.Length);
                }
            }
            if (parentLength > 0)
            {
                _parent = _value.Substring(parentStart, parentLength);
            }
        }

        public string Value => _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryParseMouseState(ShortSpan span, out MouseState state)
        {
            if (_value[span.Start] != 'M')
            {
                state = default;
                return false;
            }

            return TryParseMouseStateSlow(span, out state);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryParseMouseStateSlow(ShortSpan span, out MouseState state)
        {
            ReadOnlySpan<char> str = Span(span);
            if (str.Equals("MouseUsual", StringComparison.Ordinal))
            {
                state = Interactivity.MouseState.Normal;
            }
            else if (str.Equals("MouseOver", StringComparison.Ordinal))
            {
                state = Interactivity.MouseState.Over;
            }
            else if (str.Equals("MouseClick", StringComparison.Ordinal))
            {
                state = Interactivity.MouseState.Pressed;
            }
            else if (str.Equals("MouseLeave", StringComparison.Ordinal))
            {
                state = Interactivity.MouseState.Leave;
            }
            else
            {
                state = default;
                return false;
            }
            return true;
        }

        public ReadOnlySpan<char> OwnName => Span(_name);
        public string? Parent => _parent;

        public MouseState? MouseState { get; }

        private ReadOnlySpan<char> Span(ShortSpan span)
            => _value.AsSpan(span.Start, span.Length);

        public bool Equals(EntityName other)
            => _value.Equals(other._value, StringComparison.Ordinal);

        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value;

        private readonly struct ShortSpan
        {
            public readonly ushort Start;
            public readonly ushort Length;

            public ShortSpan(int start, int length)
                => (Start, Length) = ((ushort)start, (ushort)length);

            public static ShortSpan Empty => default;
        }

        private struct PartEnumerable
        {
            private readonly string _value;
            private readonly char _separator;

            public PartEnumerable(string value, char separator)
            {
                _value = value;
                _separator = separator;
            }

            public PartEnumerator GetEnumerator()
                => new PartEnumerator(_value, _separator);
        }

        private struct PartEnumerator
        {
            private readonly string _value;
            private readonly char _separator;

            public PartEnumerator(string value, char separator)
            {
                _value = value;
                _separator = separator;
                Current = ShortSpan.Empty;
            }

            public bool MoveNext()
            {
                ShortSpan prev = Current;
                if ((prev.Start + prev.Length) == _value.Length)
                {
                    goto exit;
                }
                int start = prev.Length > 0
                    ? prev.Start + prev.Length + 1 : 0;
                int end = _value.IndexOf(_separator, start);
                if (end == -1) { end = _value.Length; }
                Current = new ShortSpan(start, end - start);
                return true;

            exit:
                Current = ShortSpan.Empty;
                return false;
            }

            public ShortSpan Current { get; private set; }
        }
    }
}
