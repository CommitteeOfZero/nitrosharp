using System.Numerics;
using NitroSharp.Utilities;

namespace NitroSharp.Primitives
{
    /// <summary>
    /// A read-only rectangle struct that uses floating-point numbers to represent the location and size.
    /// Meant to be used in conjuction with the 'in' modifier.
    /// </summary>
    internal readonly struct RectangleF
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

        public bool Contains(in Point2D point)
        {
            return point.X >= X
                && point.X <= Right
                && point.Y >= Y
                && point.Y <= Bottom;
        }

        public RectangleF(in Vector2 position, in SizeF size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
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
            return HashHelper.Combine(
                X.GetHashCode(), Y.GetHashCode(),
                Width.GetHashCode(), Height.GetHashCode());
        }

        public static bool operator ==(in RectangleF a, in RectangleF b) => a.Equals(b);
        public static bool operator !=(in RectangleF a, in RectangleF b) => !a.Equals(b);

        public override string ToString()
        {
            return $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";
        }

        public static RectangleF Transform(in RectangleF rect, in Matrix3x2 matrix)
        {
            var position = Vector2.Transform(new Vector2(rect.X, rect.Y), matrix);
            var size = Vector2.TransformNormal(new Vector2(rect.Width, rect.Height), matrix);
            return new RectangleF(position.X, position.Y, size.X, size.Y);
        }
    }
}
