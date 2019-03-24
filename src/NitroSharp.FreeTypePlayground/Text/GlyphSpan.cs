using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NitroSharp.Text
{
    internal readonly struct GlyphSpan : IEquatable<GlyphSpan>
    {
        public GlyphSpan(uint start, uint length)
        {
            Start = start;
            Length = length;
        }

        public uint Start { get; }
        public uint Length { get; }

        public uint End => Start + Length;
        public bool IsEmpty => Length == 0;
        public bool Contains(int position) => Start <= position && position < End;
        public bool Contains(GlyphSpan span) => span.Start >= Start && span.End <= End;

        public bool Equals(GlyphSpan other) => Start == other.Start && Length == other.Length;
        public override bool Equals(object obj) => obj is GlyphSpan span && Equals(span);
        public override string ToString() => $"[{Start}..{End})";

        public static bool operator ==(GlyphSpan left, GlyphSpan right) => left.Equals(right);
        public static bool operator !=(GlyphSpan left, GlyphSpan right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GlyphSpan FromBounds(uint start, uint end)
        {
            Debug.Assert(end >= start);
            return new GlyphSpan(start, end - start);
        }


        public override int GetHashCode() => HashCode.Combine(Start, Length);
    }
}
