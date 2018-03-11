using System;
using System.Numerics;
using NitroSharp.Utilities;

namespace NitroSharp.Primitives
{
    public readonly struct SizeF : IEquatable<Size>
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

        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Size size && Equals(size);

        public override int GetHashCode() => HashHelper.Combine(Width.GetHashCode(), Height.GetHashCode());

        public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);
        public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);

        public Vector2 ToVector() => new Vector2(Width, Height);
    }
}
