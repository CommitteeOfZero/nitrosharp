using System;

namespace NitroSharp.NsScript.Text
{
    public struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
    {
        public TextSpan(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start + length < start)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int End => Start + Length;
        public int Length { get; }

        public bool IsEmpty => Length == 0;
        public bool Contains(int position) => Start <= position && position < End;
        public bool Contains(TextSpan span) => span.Start >= Start && span.End <= End;

        public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;
        public override bool Equals(object obj) => obj is TextSpan span && Equals(span);
        public override string ToString() => $"[{Start}..{End})";

        public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);
        public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);

        public int CompareTo(TextSpan other)
        {
            var diff = Start - other.Start;
            if (diff != 0)
            {
                return diff;
            }

            return Length - other.Length;
        }

        public override int GetHashCode()
        {
            int hashCode = -1730557556;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }
    }
}
