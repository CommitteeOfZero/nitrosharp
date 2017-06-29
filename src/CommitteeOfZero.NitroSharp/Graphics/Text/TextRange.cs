using System;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public struct TextRange : IEquatable<TextRange>
    {
        public TextRange(int rangeStart, int length)
        {
            RangeStart = rangeStart;
            Length = length;
        }

        public int RangeStart { get; }
        public int Length { get; }

        public bool Equals(TextRange other) => EqualsImpl(this, other);
        public override bool Equals(object obj) => EqualsImpl(this, (TextRange)obj);

        public static bool operator ==(TextRange a, TextRange b) => EqualsImpl(a, b);
        public static bool operator !=(TextRange a, TextRange b) => !EqualsImpl(a, b);

        private static bool EqualsImpl(TextRange a, TextRange b)
        {
            return a.RangeStart == b.RangeStart && a.Length == b.Length;
        }

        public override int GetHashCode() => (RangeStart, Length).GetHashCode();
        public override string ToString() => $"<{RangeStart}, {RangeStart + Length}>";
    }
}
