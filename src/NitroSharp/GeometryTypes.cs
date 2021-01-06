using System;
using System.Numerics;

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
            => (X, Y, Z) = (x, y, z);

        public float X;
        public float Y;
        public float Z;
    }

    public readonly struct Point2D : IEquatable<Point2D>
    {
        public static Point2D Zero => default;

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
        public static Point2DU Zero => default;

        public readonly uint X;
        public readonly uint Y;

        public Point2DU(uint x, uint y) => (X, Y) = (x, y);

        public bool Equals(Point2DU other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2DU other && Equals(other);

        public Vector2 ToVector2() => new(X, Y);

        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string? ToString() => $"X: {X.ToString()}, Y: {Y.ToString()}";

        public static bool operator ==(Point2DU left, Point2DU right) => left.Equals(right);
        public static bool operator !=(Point2DU left, Point2DU right) => !left.Equals(right);
    }

    [Persistable]
    public readonly partial struct Size : IEquatable<Size>
    {
        public static Size Zero => new(0, 0);

        public readonly uint Width;
        public readonly uint Height;

        public Size(uint width, uint height)
            => (Width, Height) = (width, height);

        public Size(uint value)
            => Width = Height = value;

        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object? obj) => obj is Size size && Equals(size);

        public Size Constrain(Size size)
            => new(Math.Min(Width, size.Width), Math.Min(Height, size.Height));

        public SizeF ToSizeF() => new(Width, Height);
        public Vector2 ToVector2() => new(Width, Height);

        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string? ToString() => $"{{Width:{Width.ToString()}, Height:{Height.ToString()}}}";

        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);
    }

    [Persistable]
    public readonly partial struct SizeF : IEquatable<SizeF>
    {
        public static SizeF Zero => default;

        private readonly Vector2 _value;

        public float Width => _value.X;
        public float Height => _value.Y;

        public SizeF(float width, float height)
            => _value = new Vector2(width, height);

        public SizeF(float value)
            => _value = new Vector2(value);

        public bool Equals(SizeF other) => _value.Equals(_value);
        public override bool Equals(object? obj) => obj is SizeF size && Equals(size);

        public override int GetHashCode() => _value.GetHashCode();
        public override string? ToString() => $"{{Width:{Width.ToString()}, Height:{Height.ToString()}}}";

        public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);
        public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);

        public Vector2 ToVector2() => _value;
        public Size ToSize() => new((uint)MathF.Ceiling(Width), (uint)MathF.Ceiling(Height));
    }

    [Persistable]
    internal readonly partial struct Rectangle : IEquatable<Rectangle>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            (X, Y) = (x, y);
            (Width, Height) = (width, height);
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

    [Persistable]
    internal readonly partial struct RectangleU : IEquatable<RectangleU>
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Width;
        public readonly uint Height;

        public uint Left => X;
        public uint Right => X + Width;
        public uint Top => Y;
        public uint Bottom => Y + Height;

        public Vector2 Position => new(X, Y);
        public Size Size => new(Width, Height);

        public RectangleU(uint x, uint y, uint width, uint height)
        {
            (X, Y) = (x, y);
            (Width, Height) = (width, height);
        }

        public RectangleU(Point2DU origin, Size size)
        {
            (X, Y) = (origin.X, origin.Y);
            (Width, Height) = (size.Width, size.Height);
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
        public bool Equals(in RectangleU other) =>
            X == other.X && Y == other.Y
            && Width == other.Width && Height == other.Height;

        public bool Equals(RectangleU other) =>
            X == other.X && Y == other.Y
            && Width == other.Width && Height == other.Height;

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(in RectangleU a, in RectangleU b) => a.Equals(b);
        public static bool operator !=(in RectangleU a, in RectangleU b) => !a.Equals(b);

        public override string ToString() => $"{{X:{X}, Y:{Y}, Width:{Width}, Height:{Height}}}";
    }

    [Persistable]
    internal readonly partial struct RectangleF : IEquatable<RectangleF>
    {
        private readonly Vector4 _value;

        public float X => _value.X;
        public float Y => _value.Y;
        public float Width => _value.Z;
        public float Height => _value.W;

        public RectangleF(float x, float y, float width, float height)
            => _value = new Vector4(x, y, width, height);

        public RectangleF(Vector2 position, SizeF size)
            => _value = new Vector4(position, size.Width, size.Height);

        public RectangleF(Vector2 position, Size size)
            => _value = new Vector4(position, size.Width, size.Height);

        public RectangleF(Vector2 position, Vector2 size)
            => _value = new Vector4(position, size.X, size.Y);

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Vector2 TopLeft => new(Left, Top);
        public Vector2 TopRight => new(Right, Top);
        public Vector2 BottomLeft => new(Left, Bottom);
        public Vector2 BottomRight => new(Right, Bottom);

        public Vector2 Position => new(X, Y);
        public SizeF Size => new(Width, Height);

        public bool Contains(Vector2 point)
        {
            return point.X >= X
                && point.X <= Right
                && point.Y >= Y
                && point.Y <= Bottom;
        }

        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
            => new(new Vector2(left, top), new SizeF(right - left, bottom - top));

        public static RectangleF Union(in RectangleF a, in RectangleF b)
        {
            float x1 = MathF.Min(a.X, b.X);
            float x2 = MathF.Max(a.X + a.Width, b.X + b.Width);
            float y1 = MathF.Min(a.Y, b.Y);
            float y2 = MathF.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object? obj) => obj is RectangleF rect && Equals(in rect);
        public bool Equals(in RectangleF other) => _value.Equals(other._value);
        public bool Equals(RectangleF other) => _value.Equals(other._value);
        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(in RectangleF a, in RectangleF b) => a.Equals(b);
        public static bool operator !=(in RectangleF a, in RectangleF b) => !a.Equals(b);

        public override string ToString() => $"{{X:{X}, Y:{Y}, Width:{Width}, Height:{Height}}}";
    }
}
