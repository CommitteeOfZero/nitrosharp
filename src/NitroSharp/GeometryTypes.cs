using System;
using System.Numerics;

#nullable enable

namespace NitroSharp
{
    public struct SimpleVector2
    {
        public float X;
        public float Y;
    }

    public struct SimpleVector3
    {
        public SimpleVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X;
        public float Y;
        public float Z;
    }

    public readonly struct Point2D : IEquatable<Point2D>
    {
        public readonly int X;
        public readonly int Y;

        public Point2D(int x, int y) => (X, Y) = (x, y);

        public bool Equals(Point2D other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string? ToString() => $"X: {X.ToString()}, Y: {Y.ToString()}";

        public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);
        public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);
    }

    public readonly struct Point2DU : IEquatable<Point2DU>
    {
        public readonly uint X;
        public readonly uint Y;

        public Point2DU(uint x, uint y) => (X, Y) = (x, y);

        public bool Equals(Point2DU other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2DU other && Equals(other);

        public Vector2 ToVector2() => new Vector2(X, Y);

        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string? ToString() => $"X: {X.ToString()}, Y: {Y.ToString()}";

        public static bool operator ==(Point2DU left, Point2DU right) => left.Equals(right);
        public static bool operator !=(Point2DU left, Point2DU right) => !left.Equals(right);
    }

    public readonly struct Size : IEquatable<Size>
    {
        public static readonly Size Zero = new Size(0, 0);

        public readonly uint Width;
        public readonly uint Height;

        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        public Size(uint value)
        {
            Width = Height = value;
        }

        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object? obj) => obj is Size size && Equals(size);

        public SizeF ToSizeF() => new SizeF(Width, Height);
        public Vector2 ToVector2() => new Vector2(Width, Height);

        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string? ToString() => $"{{Width:{Width.ToString()}, Height:{Height.ToString()}}}";


        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);
    }

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
        public override bool Equals(object? obj) => obj is SizeF size && Equals(size);

        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string? ToString() => $"{{Width:{Width.ToString()}, Height:{Height.ToString()}}}";

        public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);
        public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);

        public Vector2 ToVector() => new Vector2(Width, Height);
    }

    internal readonly struct Rectangle : IEquatable<Rectangle>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;

        public static Rectangle Union(in Rectangle a, in Rectangle b)
        {
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object? obj) => obj is Rectangle rect && Equals(in rect);
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
            => HashCode.Combine(X, Y, Width, Height);

        public override string ToString()
        {
            return $"{{X:{X}, Y:{Y}, Width:{Width}, Height:{Height}}}";
        }

        public static bool operator ==(in Rectangle a, in Rectangle b) => a.Equals(b);
        public static bool operator !=(in Rectangle a, in Rectangle b) => !a.Equals(b);
    }

    internal readonly struct RectangleU : IEquatable<RectangleU>
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Width;
        public readonly uint Height;

        public uint Left => X;
        public uint Right => X + Width;
        public uint Top => Y;
        public uint Bottom => Y + Height;

        public RectangleU(uint x, uint y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public static RectangleU Union(in RectangleU a, in RectangleU b)
        {
            uint x1 = Math.Min(a.X, b.X);
            uint x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            uint y1 = Math.Min(a.Y, b.Y);
            uint y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleU(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object? obj) => obj is RectangleU rect && Equals(in rect);
        public bool Equals(in RectangleU other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public bool Equals(RectangleU other)
        {
            return X == other.X && Y == other.Y
                && Width == other.Width && Height == other.Height;
        }

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Width, Height);

        public override string ToString()
        {
            return $"{{X:{X}, Y:{Y}, Width:{Width}, Height:{Height}}}";
        }

        public static bool operator ==(in RectangleU a, in RectangleU b) => a.Equals(b);
        public static bool operator !=(in RectangleU a, in RectangleU b) => !a.Equals(b);
    }

    internal readonly struct RectangleF : IEquatable<RectangleF>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleF(Vector2 position, SizeF size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public RectangleF(Vector2 position, Size size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public RectangleF(Vector2 position, Vector2 size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        public Vector2 BottomRight => new Vector2(Right, Bottom);

        public Vector2 Position => new Vector2(X, Y);
        public SizeF Size => new SizeF(Width, Height);

        public bool Contains(Vector2 point)
        {
            return point.X >= X
                && point.X <= Right
                && point.Y >= Y
                && point.Y <= Bottom;
        }

        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
            => new RectangleF(new Vector2(left, top), new SizeF(right - left, bottom - top));

        public static RectangleF Union(in RectangleF a, in RectangleF b)
        {
            float x1 = MathF.Min(a.X, b.X);
            float x2 = MathF.Max(a.X + a.Width, b.X + b.Width);
            float y1 = MathF.Min(a.Y, b.Y);
            float y2 = MathF.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object? obj) => obj is RectangleF rect && Equals(in rect);
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
            return $"{{X:{X}, Y:{Y}, Width:{Width}, Height:{Height}}}";
        }
    }
}
