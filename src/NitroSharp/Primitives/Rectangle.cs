using System;
using NitroSharp.Utilities;

namespace NitroSharp.Primitives
{
    /// <summary>
    /// A read-only rectangle struct. Meant to be used in conjuction with the 'in' modifier.
    /// </summary>
    public readonly struct Rectangle : IEquatable<Rectangle>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(in Point2D point)
        {
            return point.X >= X
                && point.X <= Right
                && point.Y >= Y
                && point.Y <= Bottom;
        }

        public RectangleF ToRectangleF()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public override bool Equals(object obj) => obj is Rectangle rect && Equals(in rect);
        public bool Equals(in Rectangle other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public bool Equals(Rectangle other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashHelper.Combine(
                X.GetHashCode(), Y.GetHashCode(),
                Width.GetHashCode(), Height.GetHashCode());
        }

        public override string ToString()
        {
            return $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";
        }

        public static bool operator ==(in Rectangle a, in Rectangle b) => a.Equals(b);
        public static bool operator !=(in Rectangle a, in Rectangle b) => !a.Equals(b);
    }
}
