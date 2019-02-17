using NitroSharp.Utilities;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScript.Text
{
    public readonly struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
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
        public bool IsEmpty => Length == 0;
        public bool Contains(int position) => Start <= position && position < End;
        public bool Contains(TextSpan span) => span.Start >= Start && span.End <= End;

        public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;
        public override bool Equals(object obj) => obj is TextSpan span && Equals(span);
        public override string ToString() => $"[{Start}..{End})";

        public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);
        public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public override int GetHashCode() => HashHelper.Combine(Start, Length);
    }
}
