using System;
using System.Collections.Immutable;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal abstract class Animation
    {
        protected readonly TimeSpan _duration;
        protected readonly NsEaseFunction _easeFunction;
        protected readonly bool _repeat;

        private float _elapsed;
        private bool _initialized;
        private bool _completed;

        protected Animation(TimeSpan duration, NsEaseFunction easeFunction, bool repeat = false)
        {
            if (duration > TimeSpan.FromMilliseconds(0))
            {
                _duration = duration;
            }
            else
            {
                _duration = TimeSpan.FromMilliseconds(1);
                _elapsed = 1.0f;
            }
            _easeFunction = easeFunction;
            _repeat = repeat;
        }

        protected float Progress
            => MathUtil.Clamp(_elapsed / (float)_duration.TotalMilliseconds, 0.0f, 1.0f);

        public bool HasCompleted => _elapsed >= _duration.TotalMilliseconds;

        public bool Update(float deltaMilliseconds)
        {
            if (_initialized)
            {
                _elapsed += deltaMilliseconds;
            }
            else
            {
                _initialized = true;
            }

            if (!_completed)
            {
                Advance(deltaMilliseconds);
                PostAdvance();
                return !_completed;
            }

            return false;
        }

        protected virtual void Advance(float dt)
        {
        }

        private void PostAdvance()
        {
            if (HasCompleted)
            {
                if (_repeat)
                {
                    _elapsed = 0;
                    _initialized = false;
                }
                else
                {
                    _completed = true;
                }
            }
        }

        protected static float GetFactor(float progress, NsEaseFunction easeFunction)
        {
            return easeFunction switch
            {
                NsEaseFunction.QuadraticEaseIn => MathF.Pow(progress, 2),
                NsEaseFunction.CubicEaseIn => MathF.Pow(progress, 3),
                NsEaseFunction.QuarticEaseIn => MathF.Pow(progress, 4),
                NsEaseFunction.QuadraticEaseOut => 1.0f - MathF.Pow(1.0f - progress, 2),
                NsEaseFunction.CubicEaseOut => 1.0f - MathF.Pow(1.0f - progress, 3),
                NsEaseFunction.QuarticEaseOut => 1.0f - MathF.Pow(1.0f - progress, 4),
                NsEaseFunction.SineEaseIn => 1.0f - MathF.Cos(progress * MathF.PI * 0.5f),
                NsEaseFunction.SineEaseOut => MathF.Sin(progress * MathF.PI * 0.5f),
                NsEaseFunction.SineEaseInOut => 0.5f * (1.0f - MathF.Cos(progress * MathF.PI)),
                NsEaseFunction.SineEaseOutIn => MathF.Acos(1.0f - progress * 2.0f) / MathF.PI,
                _ => progress,
            };
        }
    }

    internal abstract class PropertyAnimation<TEntity, TProperty> : Animation
        where TEntity : Entity
        where TProperty : struct
    {
        protected readonly TEntity _entity;

        protected PropertyAnimation(
            TEntity entity, TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false) : base(duration, easeFunction, repeat)
        {
            _entity = entity;
        }

        protected abstract ref TProperty GetRef();

        protected override void Advance(float dt)
        {
            InterpolateValue(ref GetRef(), GetFactor(Progress, _easeFunction));
        }

        protected abstract void InterpolateValue(ref TProperty value, float factor);
    }

    internal abstract class FloatAnimation<TEntity> : PropertyAnimation<TEntity, float>
        where TEntity : Entity
    {
        protected readonly float _startValue;
        protected readonly float _endValue;

        protected FloatAnimation(
            TEntity entity,
            float startValue, float endValue,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false) : base(entity, duration, easeFunction, repeat)
        {
            (_startValue, _endValue) = (startValue, endValue);
        }

        protected override void InterpolateValue(ref float value, float factor)
        {
            float delta = _endValue - _startValue;
            value = _startValue + delta * factor;
        }
    }

    //internal sealed class OpacityAnimation : FloatAnimation<RenderItem2D>
    //{
    //    public OpacityAnimation(
    //        RenderItem2D entity,
    //        float startValue, float endValue,
    //        TimeSpan duration,
    //        NsEaseFunction easeFunction = NsEaseFunction.None,
    //        bool repeat = false)
    //        : base(entity, startValue, endValue, duration, easeFunction, repeat)
    //    {
    //    }
    //
    //    //protected override ref float GetRef() => ref _entity.Color.
    //}

    internal abstract class Vector3Animation<TEntity> : PropertyAnimation<TEntity, Vector3>
        where TEntity : Entity
    {
        protected readonly Vector3 _startValue;
        protected readonly Vector3 _endValue;

        protected Vector3Animation(
            TEntity entity,
            in Vector3 startValue, in Vector3 endValue,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false) : base(entity, duration, easeFunction, repeat)
        {
            (_startValue, _endValue) = (startValue, endValue);
        }

        protected override void InterpolateValue(ref Vector3 value, float factor)
        {
            Vector3 delta = _endValue - _startValue;
            value = _startValue + delta * factor;
        }
    }

    internal sealed class MoveAnimation : Vector3Animation<RenderItem>
    {
        public MoveAnimation(
            RenderItem entity,
            in Vector3 startPosition, in Vector3 destination,
            TimeSpan duration,
            NsEaseFunction easeFunction = NsEaseFunction.None,
            bool repeat = false)
            : base(entity, startPosition, destination, duration, easeFunction, repeat)
        {
        }

        protected override ref Vector3 GetRef() => ref _entity.Transform.Position;
    }

    internal sealed class ScaleAnimation : Vector3Animation<RenderItem>
    {
        public ScaleAnimation(
            RenderItem entity,
            in Vector3 startScale, in Vector3 endScale,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false)
            : base(entity, startScale, endScale, duration, easeFunction, repeat)
        {
        }

        protected override ref Vector3 GetRef() => ref _entity.Transform.Scale;
    }

    internal sealed class RotateAnimation : Vector3Animation<RenderItem>
    {
        public RotateAnimation(
            RenderItem entity,
            in Vector3 startRot, in Vector3 endRot,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false)
            : base(entity, startRot, endRot, duration, easeFunction, repeat)
        {
        }

        protected override ref Vector3 GetRef() => ref _entity.Transform.Rotation;
    }

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
            => (P0, P1, P2, P3) = (p0, p1, p2, p3);

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

    internal sealed class BezierMoveAnimation : PropertyAnimation<RenderItem2D, Vector3>
    {
        public BezierMoveAnimation(
            RenderItem2D entity, TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false)
            : base(entity, duration, easeFunction, repeat)
        {
        }

        public ProcessedBezierCurve Curve { get; set; }

        protected override ref Vector3 GetRef() => ref _entity.Transform.Position;

        protected override void InterpolateValue(ref Vector3 value, float factor)
        {
            int segCount = Curve.Segments.Length;
            int segIndex = (int)MathUtil.Clamp(factor * segCount, 0, segCount - 1);
            float t = factor * segCount - segIndex;
            ProcessedBezierSegment seg = Curve.Segments[segIndex];
            value = new Vector3(seg.CalcPoint(t), value.Z);
        }
    }
}
