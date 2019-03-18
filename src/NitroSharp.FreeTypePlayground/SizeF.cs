using System;
using System.Numerics;

namespace NitroSharp.Primitives
{
    public readonly struct SizeF : IEquatable<SizeF>
    {
        public static readonly SizeF Zero = new SizeF(0.0f, 0.0f);

        public readonly float Width;
        public readonly float Height;

        public SizeF(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public SizeF(float value)
        {
            Width = Height = value;
        }

        public static SizeF FromVector(in Vector2 vector) => new SizeF(vector.X, vector.Y);

        public bool Equals(SizeF other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is SizeF size && Equals(size);

        public override int GetHashCode() => HashCode.Combine(Width, Height);

        public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);
        public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);

        public Vector2 ToVector() => new Vector2(Width, Height);
    }
}
