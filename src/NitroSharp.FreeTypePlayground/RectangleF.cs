using System;
using System.Numerics;
using NitroSharp.Utilities;

namespace NitroSharp.Primitives
{
    internal readonly struct RectangleF : IEquatable<RectangleF>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleF(in Vector2 position, in SizeF size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= X
                && point.X <= Right
                && point.Y >= Y
                && point.Y <= Bottom;
        }

        public override bool Equals(object obj) => obj is RectangleF rect && Equals(in rect);
        public bool Equals(in RectangleF other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public bool Equals(RectangleF other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(in RectangleF a, in RectangleF b) => a.Equals(b);
        public static bool operator !=(in RectangleF a, in RectangleF b) => !a.Equals(b);

        public override string ToString()
        {
            return $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";
        }
    }
}
