using System;
using MessagePack;
using System.Collections.Immutable;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;

namespace NitroSharp;

internal enum AnimationKind
{
    Move,
    Zoom,
    Rotate,
    BezierMove,
    Transition,
    Fade
}

internal abstract class Animation
{
    protected enum AdvanceResult
    {
        KeepGoing,
        Stop
    }

    private readonly bool _repeat;
    protected readonly NsEaseFunction EaseFunction;

    private float _elapsed;
    private bool _initialized;
    private bool _completed;

    protected Animation(NsEaseFunction easeFunction = NsEaseFunction.Linear, bool repeat = false)
    {
        EaseFunction = easeFunction;
        _repeat = repeat;
    }

    protected float Elapsed => _elapsed;

    public virtual bool Update(float dt)
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
            if (Advance() == AdvanceResult.Stop)
            {
                if (_repeat)
                {
                    Reset();
                }
                else
                {
                    _completed = true;
                }
            }
            return !_completed;
        }

        return false;
    }

    public void Reset()
    {
        _elapsed = 0;
        _initialized = false;
    }

    protected abstract AdvanceResult Advance();

    protected static float GetFactor(float progress, NsEaseFunction easeFunction) => easeFunction switch
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
        _ => progress
    };
}

internal abstract class AnimationWithDuration : Animation
{
    private readonly TimeSpan _duration;

    protected AnimationWithDuration(
        TimeSpan duration,
        NsEaseFunction easeFunction = NsEaseFunction.Linear,
        bool repeat = false)
        : base(easeFunction, repeat)
    {
        _duration = duration;
    }

    public bool HasCompleted => Elapsed >= _duration.TotalMilliseconds;

    protected float Progress
        => Math.Clamp(Elapsed / (float)_duration.TotalMilliseconds, 0.0f, 1.0f);

    protected override AdvanceResult Advance()
    {
        return HasCompleted
            ? AdvanceResult.Stop
            : AdvanceResult.KeepGoing;
    }
}

internal abstract class ValueAnimation<TValue> : AnimationWithDuration
    where TValue : struct
{
    protected ValueAnimation(
        TimeSpan duration,
        NsEaseFunction easeFunction = NsEaseFunction.Linear,
        bool repeat = false)
        : base(duration, easeFunction, repeat)
    {
    }

    protected abstract ref TValue GetValueRef();

    protected override AdvanceResult Advance()
    {
        InterpolateValue(ref GetValueRef(), GetFactor(Progress, EaseFunction));
        return base.Advance();
    }

    protected abstract void InterpolateValue(ref TValue value, float factor);
}

internal abstract class PropertyAnimation<TEntity, TProperty> : ValueAnimation<TProperty>
    where TProperty : struct
{
    protected readonly TEntity _entity;

    protected PropertyAnimation(
        TEntity entity,
        TimeSpan duration,
        NsEaseFunction easeFunction, bool repeat = false)
        : base(duration, easeFunction, repeat)
    {
        _entity = entity;
    }
}

internal abstract class UIntAnimation<TObject> : PropertyAnimation<TObject, uint>
{
    private readonly uint _startValue;
    private readonly uint _endValue;

    protected UIntAnimation(
        TObject entity,
        uint startValue, uint endValue,
        TimeSpan duration,
        NsEaseFunction easeFunction = NsEaseFunction.Linear,
        bool repeat = false)
        : base(entity, duration, easeFunction, repeat)
    {
        (_startValue, _endValue) = (startValue, endValue);
    }

    protected override void InterpolateValue(ref uint value, float factor)
    {
        uint delta = _endValue - _startValue;
        value = (uint)(_startValue + delta * factor);
    }
}

internal readonly struct ProcessedBezierCurve
{
    public readonly ImmutableArray<ProcessedBezierSegment> Segments;

    public ProcessedBezierCurve(ImmutableArray<ProcessedBezierSegment> segments)
    {
        Segments = segments;
    }

    public ProcessedBezierCurve(ref MessagePackReader reader)
    {
        int length = reader.ReadArrayHeader();
        var segments = ImmutableArray.CreateBuilder<ProcessedBezierSegment>(length);
        for (int i = 0; i < length; i++)
        {
            segments.Add(new ProcessedBezierSegment(ref reader));
        }

        Segments = segments.ToImmutable();
    }

    public void Serialize(ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(Segments.Length);
        foreach (ProcessedBezierSegment seg in Segments)
        {
            seg.Serialize(ref writer);
        }
    }
}

internal sealed class OpacityAnimation : AnimationWithDuration
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

    protected override AdvanceResult Advance()
    {
        float factor = GetFactor(Progress, EaseFunction);
        float delta = _endOpacity - _startOpacity;
        float current = _startOpacity + delta * factor;
        _entity.Color.SetAlpha(current);
        return base.Advance();
    }
}

internal abstract class Vector3Animation<TEntity> : PropertyAnimation<TEntity, Vector3>
    where TEntity : Entity
{
    private readonly Vector3 _startValue;
    private readonly Vector3 _endValue;

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
        NsEaseFunction easeFunction = NsEaseFunction.Linear,
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

[Persistable]
internal readonly partial struct ProcessedBezierSegment
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
