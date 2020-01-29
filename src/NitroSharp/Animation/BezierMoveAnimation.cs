using System;
using System.Collections.Immutable;
using System.Numerics;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp.Animation
{
    internal readonly struct ProcessedBezierCurve
    {
        public readonly ImmutableArray<ProcessedBezierSegment> Segments;

        public ProcessedBezierCurve(ImmutableArray<ProcessedBezierSegment> segments)
        {
            Segments = segments;
        }
    }

    internal readonly struct ProcessedBezierSegment
    {
        public readonly Vector2 P0;
        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly Vector2 P3;

        public ProcessedBezierSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public Vector2 CalcPoint(float t)
        {
            float a = 1 - t;
            float aSquared = a * a;
            float aCubed = aSquared * a;
            float b = t;
            float bSquared = b * b;
            float bCubed = bSquared * b;
            return P0 * aCubed
                + P1 * 3 * aSquared * b
                + P2 * 3 * a * bSquared
                + P3 * bCubed;
        }
    }

    internal sealed class BezierMoveAnimation : LerpAnimation<TransformComponents>
    {
        public BezierMoveAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
        {
        }

        public ProcessedBezierCurve Curve { get; set; }

        protected override EntityStorage.ComponentVec<TransformComponents> SelectComponentVec()
        {
            var storage = World.GetStorage<RenderItemStorage>(Entity);
            return storage.TransformComponents;
        }

        protected override void InterpolateValue(ref TransformComponents value, float factor)
        {
            int segCount = Curve.Segments.Length;
            int segIndex = (int)MathUtil.Clamp(factor * segCount, 0, segCount - 1);
            float t = factor * segCount - segIndex;
            ProcessedBezierSegment seg = Curve.Segments[segIndex];
            value.Position = new Vector3(seg.CalcPoint(t), value.Position.Z);
        }
    }
}
