using System;
using System.Collections.Immutable;
using MessagePack;

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

        public CompositeBezier(ref MessagePackReader reader)
        {
            int length = reader.ReadArrayHeader();
            var segments = ImmutableArray.CreateBuilder<CubicBezierSegment>(length);
            for (int i = 0; i < length; i++)
            {
                segments.Add(new CubicBezierSegment(ref reader));
            }

            Segments = segments.ToImmutable();
        }

        public void Serialize(ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(Segments.Length);
            foreach (CubicBezierSegment seg in Segments)
            {
                seg.Serialize(ref writer);
            }
        }
    }

    [Persistable]
    public readonly partial struct CubicBezierSegment
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

        public override int GetHashCode() => HashCode.Combine(P0, P1, P2, P3);
    }

    [Persistable]
    public readonly partial struct BezierControlPoint
    {
        public readonly NsCoordinate X;
        public readonly NsCoordinate Y;

        public BezierControlPoint(NsCoordinate x, NsCoordinate y)
            => (X, Y) = (x, y);

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
