using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Primitives
{
    public readonly struct CompositeBezier
    {
        public readonly ImmutableArray<CubicBezierSegment> Segments;

        public CompositeBezier(ImmutableArray<CubicBezierSegment> segments)
            => Segments = segments;

        public override int GetHashCode()
        {
            int code = 0;
            foreach (CubicBezierSegment segment in Segments)
            {
                code = HashCode.Combine(code, segment);
            }
            return code;
        }
    }

    public readonly struct CubicBezierSegment
    {
        public readonly BezierControlPoint P0;
        public readonly BezierControlPoint P1;
        public readonly BezierControlPoint P2;
        public readonly BezierControlPoint P3;

        public CubicBezierSegment(
            BezierControlPoint p0,
            BezierControlPoint p1,
            BezierControlPoint p2,
            BezierControlPoint p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public BezierControlPoint this[uint index] => index switch
        {
            0 => P0,
            1 => P1,
            2 => P2,
            3 => P3,
            _ => throw new IndexOutOfRangeException() 
        };

        public override int GetHashCode() => HashCode.Combine(P0, P1, P2, P3);
    }

    public readonly struct BezierControlPoint
    {
        public readonly NsCoordinate X;
        public readonly NsCoordinate Y;

        public BezierControlPoint(NsCoordinate x, NsCoordinate y)
            => (X, Y) = (x, y);

        public void Deconstruct(out NsCoordinate x, out NsCoordinate y)
        {
            x = X;
            y = Y;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
