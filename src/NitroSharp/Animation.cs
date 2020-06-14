using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;
using Veldrid;

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
            Debug.Assert(duration > TimeSpan.Zero);
            _duration = duration;
            _easeFunction = easeFunction;
            _repeat = repeat;
        }

        protected float Progress
            => MathUtil.Clamp(_elapsed / (float)_duration.TotalMilliseconds, 0.0f, 1.0f);

        public bool HasCompleted => _elapsed >= _duration.TotalMilliseconds;

        public bool Update(float dt)
        {
            if (_initialized)
            {
                _elapsed += dt;
            }
            else
            {
                _initialized = true;
            }

            if (!_completed)
            {
                Advance(dt);
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

    internal enum AnimationKind
    {
        Move,
        Zoom,
        Rotate,
        BezierMove,
        Transition,
        Fade
    }

    internal abstract class ValueAnimation<TEntity, TValue> : Animation
        where TEntity : Entity
        where TValue : struct
    {
        protected readonly TEntity _entity;

        protected ValueAnimation(
            TEntity entity, TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false) : base(duration, easeFunction, repeat)
        {
            _entity = entity;
        }

        protected abstract ref TValue GetValueRef();

        protected override void Advance(float dt)
        {
            InterpolateValue(ref GetValueRef(), GetFactor(Progress, _easeFunction));
        }

        protected abstract void InterpolateValue(ref TValue value, float factor);
    }

    internal abstract class FloatAnimation<TEntity> : ValueAnimation<TEntity, float>
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

    internal sealed class TransitionAnimation : Animation, IDisposable
    {
        private readonly float _srcFadeAmount;
        private readonly float _dstFadeAmount;
        private float _fadeAmount;

        public TransitionAnimation(
            AssetRef<Texture> mask,
            float srcFadeAmount, float dstFadeAmount,
            TimeSpan duration,
            NsEaseFunction easeFunction)
            : base(duration, easeFunction, repeat: false)
        {
            Mask = mask;
            _srcFadeAmount = srcFadeAmount;
            _dstFadeAmount = dstFadeAmount;
        }

        public AssetRef<Texture> Mask { get; }
        public float FadeAmount => _fadeAmount;

        protected override void Advance(float dt)
        {
            float delta = _dstFadeAmount - _srcFadeAmount;
            _fadeAmount = _srcFadeAmount + delta * GetFactor(Progress, _easeFunction);
        }

        public void Dispose()
        {
            //Mask.Dispose();
        }
    }

    internal sealed class OpacityAnimation : Animation
    {
        private readonly RenderItem _entity;
        private readonly float _startOpacity;
        private readonly float _endOpacity;

        public OpacityAnimation(
            RenderItem entity,
            float startOpacity, float endOpacity,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false) : base(duration, easeFunction, repeat)
        {
            _entity = entity;
            _startOpacity = startOpacity;
            _endOpacity = endOpacity;
        }

        protected override void Advance(float dt)
        {
            float factor = GetFactor(Progress, _easeFunction);
            float delta = _endOpacity - _startOpacity;
            float current = _startOpacity + delta * factor;
            _entity.Color.SetAlpha(current);
        }
    }

    internal abstract class Vector3Animation<TEntity> : ValueAnimation<TEntity, Vector3>
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

        protected override ref Vector3 GetValueRef() => ref _entity.Transform.Position;
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

        protected override ref Vector3 GetValueRef() => ref _entity.Transform.Scale;
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

        protected override ref Vector3 GetValueRef() => ref _entity.Transform.Rotation;
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

    internal sealed class BezierMoveAnimation : ValueAnimation<RenderItem2D, Vector3>
    {
        public BezierMoveAnimation(
            RenderItem2D entity,
            ProcessedBezierCurve curve,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false)
            : base(entity, duration, easeFunction, repeat)
        {
            Curve = curve;
        }

        public ProcessedBezierCurve Curve { get; }

        protected override ref Vector3 GetValueRef() => ref _entity.Transform.Position;

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
