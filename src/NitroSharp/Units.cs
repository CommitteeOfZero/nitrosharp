global using DesignDimension = NitroSharp.DimensionF<NitroSharp.DesignPixel>;
global using PhysicalDimension = NitroSharp.DimensionF<NitroSharp.ScreenPixel>;

global using DesignDimensionU = NitroSharp.DimensionU<NitroSharp.DesignPixel>;
global using PhysicalDimensionU = NitroSharp.DimensionU<NitroSharp.ScreenPixel>;

global using DesignPoint = NitroSharp.PointF<NitroSharp.DesignPixel>;
global using PhysicalPoint = NitroSharp.PointF<NitroSharp.ScreenPixel>;

global using DesignPointU = NitroSharp.PointU<NitroSharp.DesignPixel>;
global using PhysicalPointU = NitroSharp.PointU<NitroSharp.ScreenPixel>;

//global using DesignPointF = NitroSharp.PointF<NitroSharp.DesignPixel>;
//global using PhysicalPointF = NitroSharp.PointF<NitroSharp.ScreenPixel>;

global using DesignSize = NitroSharp.SizeF<NitroSharp.DesignPixel>;
global using PhysicalSize = NitroSharp.SizeF<NitroSharp.ScreenPixel>;

global using DesignMarginU = NitroSharp.MarginU<NitroSharp.DesignPixel>;
global using PhysicalMarginU = NitroSharp.MarginU<NitroSharp.ScreenPixel>;

global using DesignSizeU = NitroSharp.SizeU<NitroSharp.DesignPixel>;
global using PhysicalSizeU = NitroSharp.SizeU<NitroSharp.ScreenPixel>;

global using DesignRect = NitroSharp.RectangleF<NitroSharp.DesignPixel>;
global using PhysicalRect = NitroSharp.RectangleF<NitroSharp.ScreenPixel>;

global using DesignRectU = NitroSharp.RectangleU<NitroSharp.DesignPixel>;
global using PhysicalRectU = NitroSharp.RectangleU<NitroSharp.ScreenPixel>;

global using WorldToDeviceScale = NitroSharp.Scale<NitroSharp.DesignPixel, NitroSharp.ScreenPixel>;
global using DeviceToWorldScale = NitroSharp.Scale<NitroSharp.ScreenPixel, NitroSharp.DesignPixel>;

using System;
using System.Numerics;

namespace NitroSharp;

[Persistable]
public readonly partial record struct DimensionU<TUnit>(uint Value)
{
    public DimensionU<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(
        (uint)Math.Round(Value * scale.Factor)
    );

    public static implicit operator uint(DimensionU<TUnit> dimension) => dimension.Value;
    public static implicit operator DimensionU<TUnit>(uint value) => new(value);
}

[Persistable]
public readonly partial record struct DimensionF<TUnit>(float Value)
{
    public DimensionF<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(Value * scale.Factor);

    public static implicit operator float(DimensionF<TUnit> dimension) => dimension.Value;
    public static implicit operator DimensionF<TUnit>(float value) => new(value);
}

public readonly record struct PointU<TUnit>(uint X, uint Y)
{
    public static PointU<TUnit> Zero => default;

    public Vector2 ToVector2() => new(X, Y);

    public PointU<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(
        (uint)Math.Round(X * scale.Factor),
        (uint)Math.Round(Y * scale.Factor)
    );
}

public readonly record struct PointF<TUnit>(Vector2 Value)
{
    public PointF(float x, float y) : this(new Vector2(x, y)) { }

    public float X => Value.X;
    public float Y => Value.Y;

    public static PointF<TUnit> Zero => default;

    public Vector2 ToVector2() => Value;

    public PointF<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(Value * scale.Factor);
}

[Persistable]
public readonly partial record struct SizeU<TUnit>(uint Width, uint Height)
{
    public static SizeU<TUnit> Zero => new(0, 0);

    public DimensionU<TUnit> TypedWidth => new(Width);
    public DimensionU<TUnit> TypedHeight => new(Height);

    public SizeU<TUnit> Constrain(SizeU<TUnit> size)
        => new(Math.Min(Width, size.Width), Math.Min(Height, size.Height));

    public SizeF<TUnit> ToSizeF() => new(Width, Height);
    public Vector2 ToVector2() => new(Width, Height);

    public SizeU<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
    {
        return new SizeU<TDstUnit>(
            (uint)Math.Round(Width * scale.Factor),
            (uint)Math.Round(Height * scale.Factor)
        );
    }
}

[Persistable]
public readonly partial record struct SizeF<TUnit>(float Width, float Height)
{
    public static SizeF<TUnit> Zero => new(0, 0);

    public SizeF<TUnit> Constrain(SizeF<TUnit> size)
        => new(MathF.Min(Width, size.Width), MathF.Min(Height, size.Height));

    public SizeU<TUnit> ToSize() => new((uint)Math.Round(Width), (uint)Math.Round(Height));
    public Vector2 ToVector2() => new(Width, Height);

    public SizeF<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
        => new(Width * scale.Factor, Height * scale.Factor);
}

[Persistable]
public readonly partial record struct MarginU<TUnit>(uint Left, uint Top, uint Right, uint Bottom)
{
    public MarginU<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale) => new(
        (uint)Math.Round(Left * scale.Factor),
        (uint)Math.Round(Top * scale.Factor),
        (uint)Math.Round(Right * scale.Factor),
        (uint)Math.Round(Bottom * scale.Factor)
    );
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
internal readonly partial record struct RectangleF<TUnit>(float X, float Y, float Width, float Height)
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
    public SizeF<TUnit> Size => new(Width, Height);

    public RectangleF(PointF<TUnit> origin, SizeF<TUnit> size)
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

    public RectangleU<TUnit> ToRect() => new(
        (uint)Math.Round(X),
        (uint)Math.Round(Y),
        (uint)Math.Round(Width),
        (uint)Math.Round(Height)
    );

    public RectangleF<TDstUnit> Convert<TDstUnit>(Scale<TUnit, TDstUnit> scale)
    {
        return new RectangleF<TDstUnit>(
            X * scale.Factor,
            Y * scale.Factor,
            Width * scale.Factor,
            Height * scale.Factor
        );
    }

    public static RectangleF<TUnit> FromLTRB(float left, float top, float right, float bottom)
        => new(new PointF<TUnit>(left, top), new SizeF<TUnit>(right - left, bottom - top));

    public static RectangleF<TUnit> Union(in RectangleF<TUnit> a, in RectangleF<TUnit> b)
    {
        float x1 = MathF.Min(a.X, b.X);
        float x2 = MathF.Max(a.X + a.Width, b.X + b.Width);
        float y1 = MathF.Min(a.Y, b.Y);
        float y2 = MathF.Max(a.Y + a.Height, b.Y + b.Height);
        return new RectangleF<TUnit>(x1, y1, x2 - x1, y2 - y1);
    }
}

public struct ScreenPixel
{
}

public struct DesignPixel
{
}

public readonly record struct Scale<TSrcUnit, TDstUnit>(float Factor)
{
    public static Scale<TSrcUnit, TDstUnit> Identity => new(1.0f);
}
