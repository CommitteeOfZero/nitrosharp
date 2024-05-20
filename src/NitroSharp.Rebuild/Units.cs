global using DesignDimension = NitroSharp.Dimension<NitroSharp.DesignPixel>;
global using ScreenDimension = NitroSharp.Dimension<NitroSharp.ScreenPixel>;
global using DesignSize = NitroSharp.Size<NitroSharp.DesignPixel>;
global using ScreenSize = NitroSharp.Size<NitroSharp.ScreenPixel>;
global using DesignSizeU = NitroSharp.SizeU<NitroSharp.DesignPixel>;
global using ScreenSizeU = NitroSharp.SizeU<NitroSharp.ScreenPixel>;
global using DesignRect = NitroSharp.Rectangle<NitroSharp.DesignPixel>;
global using ScreenRect = NitroSharp.Rectangle<NitroSharp.ScreenPixel>;
global using DesignRectU = NitroSharp.RectangleU<NitroSharp.DesignPixel>;
global using ScreenRectU = NitroSharp.RectangleU<NitroSharp.ScreenPixel>;
global using TexturePointU = NitroSharp.PointU<NitroSharp.TexturePixel>;
global using TextureSizeU = NitroSharp.SizeU<NitroSharp.TexturePixel>;
global using TextureRectU = NitroSharp.RectangleU<NitroSharp.TexturePixel>;

using System;
using System.Numerics;

namespace NitroSharp;

[Persistable]
public readonly partial record struct Dimension<TUnit>(float Value)
{
    public Dimension<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(Value * scale.Factor);

    public static implicit operator float(Dimension<TUnit> dimension) => dimension.Value;
    public static implicit operator Dimension<TUnit>(float value) => new(value);
}

public readonly record struct Point<TUnit>(Vector2 Value)
{
    public Point(float x, float y) : this(new Vector2(x, y)) { }

    public float X => Value.X;
    public float Y => Value.Y;

    public static Point<TUnit> Zero => default;

    public Vector2 ToVector2() => Value;

    public Point<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(Value * scale.Factor);
}

public readonly record struct PointU<TUnit>(uint X, uint Y)
{
    public static PointU<TUnit> Zero => default;
}

public struct ScreenPixel
{
}

public struct DesignPixel
{
}

public struct TexturePixel
{
}

public readonly record struct Scale<TSrcUnit, TDstUnit>(float Factor)
{
    public static Scale<TSrcUnit, TDstUnit> Identity => new(1.0f);
}

[Persistable]
public readonly partial record struct Size<TUnit>(float Width, float Height)
{
    public static Size<TUnit> Zero => new(0, 0);

    public Size<TUnit> Constrain(Size<TUnit> size)
        => new(MathF.Min(Width, size.Width), MathF.Min(Height, size.Height));

    public Size<TUnit> ToSize() => new((uint)Math.Round(Width), (uint)Math.Round(Height));
    public Vector2 ToVector2() => new(Width, Height);

    public Size<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
        => new(Width * scale.Factor, Height * scale.Factor);

    public Size<TDstUnit> Reinterpret<TDstUnit>() => new(Width, Height);
}

[Persistable]
public readonly partial record struct SizeU<TUnit>(uint Width, uint Height)
{
    public static SizeU<TUnit> Zero => new(0, 0);

    // public SizeU<TUnit> Constrain(Size<TUnit> size)
    //     => new(MathF.Min(Width, size.Width), MathF.Min(Height, size.Height));

    public SizeU<TDstUnit> Reinterpret<TDstUnit>() => new(Width, Height);

    public SizeU<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
        => new((uint)Math.Round(Width * scale.Factor), (uint)Math.Round(Height * scale.Factor));

    public Size<TUnit> ToFloatSize() => new(Width, Height);
    public Vector2 ToVector2() => new(Width, Height);
}

[Persistable]
public readonly partial record struct RectangleU<TUnit>(uint X, uint Y, uint Width, uint Height)
{
    public static SizeU<TUnit> Zero => new(0, 0);

    public uint Left => X;
    public uint Right => X + Width;
    public uint Top => Y;
    public uint Bottom => Y + Height;

    public Vector2 Position => new(X, Y);
    public SizeU<TUnit> Size => new(Width, Height);

    public RectangleU(PointU<TUnit> origin, SizeU<TUnit> size)
        : this(origin.X, origin.Y, size.Width, size.Height)
    {
    }

    //public RectangleU(Point2DU origin, Size size)
    //{
    //    (X, Y) = (origin.X, origin.Y);
    //    (Width, Height) = (size.Width, size.Height);
    //}

    public static RectangleU<TUnit> Union(in RectangleU<TUnit> a, in RectangleU<TUnit> b)
    {
        uint x1 = Math.Min(a.X, b.X);
        uint x2 = Math.Max(a.X + a.Width, b.X + b.Width);
        uint y1 = Math.Min(a.Y, b.Y);
        uint y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
        return new RectangleU<TUnit>(x1, y1, x2 - x1, y2 - y1);
    }
}

[Persistable]
internal readonly partial record struct Rectangle<TUnit>(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;

    public Vector2 TopLeft => new(Left, Top);
    public Vector2 TopRight => new(Right, Top);
    public Vector2 BottomLeft => new(Left, Bottom);
    public Vector2 BottomRight => new(Right, Bottom);

    public Vector2 Position => new(X, Y);
    public Size<TUnit> Size => new(Width, Height);

    public Rectangle(Point<TUnit> origin, Size<TUnit> size)
        : this(origin.X, origin.Y, size.Width, size.Height)
    {
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= X
            && point.X < Right
            && point.Y >= Y
            && point.Y < Bottom;
    }

    // public RectangleU<TUnit> ToRect() => new(
    //     (uint)Math.Round(X),
    //     (uint)Math.Round(Y),
    //     (uint)Math.Round(Width),
    //     (uint)Math.Round(Height)
    // );

    public Rectangle<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
    {
        return new Rectangle<TDstUnit>(
            X * scale.Factor,
            Y * scale.Factor,
            Width * scale.Factor,
            Height * scale.Factor
        );
    }

    public static Rectangle<TUnit> FromLTRB(float left, float top, float right, float bottom)
        => new(new Point<TUnit>(left, top), new Size<TUnit>(right - left, bottom - top));

    public static Rectangle<TUnit> Union(in Rectangle<TUnit> a, in Rectangle<TUnit> b)
    {
        float x1 = MathF.Min(a.X, b.X);
        float x2 = MathF.Max(a.X + a.Width, b.X + b.Width);
        float y1 = MathF.Min(a.Y, b.Y);
        float y2 = MathF.Max(a.Y + a.Height, b.Y + b.Height);
        return new Rectangle<TUnit>(x1, y1, x2 - x1, y2 - y1);
    }
}
