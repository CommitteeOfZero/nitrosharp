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
    public readonly partial record struct CubicBezierSegment(
        BezierControlPoint P0,
        BezierControlPoint P1,
        BezierControlPoint P2,
        BezierControlPoint P3
    );

    [Persistable]
    public readonly partial record struct BezierControlPoint(NsCoordinate X, NsCoordinate Y);
}
