using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal abstract class Animation
    {
        protected enum AdvanceResult
        {
            KeepGoing,
            Stop
        }

        private readonly bool _repeat;
        protected readonly NsEaseFunction _easeFunction;

        private float _elapsed;
        private bool _initialized;
        private bool _completed;

        protected Animation(
            NsEaseFunction easeFunction = NsEaseFunction.Linear,
            bool repeat = false)
        {
            _easeFunction = easeFunction;
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
            => MathUtil.Clamp(Elapsed / (float)_duration.TotalMilliseconds, 0.0f, 1.0f);

        protected override AdvanceResult Advance()
        {
            return HasCompleted
                ? AdvanceResult.Stop
                : AdvanceResult.KeepGoing;
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
            InterpolateValue(ref GetValueRef(), GetFactor(Progress, _easeFunction));
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

    internal abstract class FloatAnimation : ValueAnimation<float>
    {
        private readonly float _startValue;
        private readonly float _endValue;

        protected FloatAnimation(
            float startValue, float endValue,
            TimeSpan duration,
            NsEaseFunction easeFunction,
            bool repeat = false)
            : base(duration, easeFunction, repeat)
        {
            (_startValue, _endValue) = (startValue, endValue);
        }

        protected override void InterpolateValue(ref float value, float factor)
        {
            float delta = _endValue - _startValue;
            value = _startValue + delta * factor;
        }
    }

    internal class TypewriterAnimation : Animation
    {
        private readonly struct AnimationPair
        {
            public readonly Animation Text;
            public readonly Animation? RubyText;

            public AnimationPair(Animation text, Animation? rubyText)
            {
                Text = text;
                RubyText = rubyText;
            }
        }

        private readonly TextLayout _textLayout;
        private Queue<AnimationPair> _anims;

        public TypewriterAnimation(
            TextLayout textLayout,
            ReadOnlySpan<GlyphRun> glyphRuns,
            float timePerGlyph)
        {
            _textLayout = textLayout;
            _anims = new Queue<AnimationPair>();

            for (int i = 0; i < glyphRuns.Length; i++)
            {
                ref readonly GlyphRun run = ref glyphRuns[i];
                if (run.IsRubyText) { continue; }
                var glyphSpan = new GlyphSpan(run.GlyphSpan.Start, 0);

                void flush()
                {
                    if (!glyphSpan.IsEmpty)
                    {
                        GlyphRunRevealAnimation anim = createAnim(glyphSpan, timePerGlyph);
                        _anims.Enqueue(new AnimationPair(anim, null));
                        glyphSpan = default;
                    }
                }

                while (i < glyphRuns.Length)
                {
                    run = ref glyphRuns[i];
                    textLayout.GetOpacityValuesMut(run.GlyphSpan).Fill(0.0f);

                    if (i < glyphRuns.Length - 1
                        && glyphRuns[i] is { IsRubyBase: true } rb
                        && glyphRuns[i + 1] is { IsRubyText: true } rt)
                    {
                        flush();
                        uint baseGlyphCount = NbNonWhitespaceGlyphs(rb.GlyphSpan);
                        uint rubyGlyphCount = NbNonWhitespaceGlyphs(rt.GlyphSpan);
                        float rubyGlyphTime = timePerGlyph * baseGlyphCount / rubyGlyphCount;
                        GlyphRunRevealAnimation baseAnim = createAnim(rb.GlyphSpan, timePerGlyph);
                        GlyphRunRevealAnimation rubyAnim = createAnim(rt.GlyphSpan, rubyGlyphTime);
                        _anims.Enqueue(new AnimationPair(baseAnim, rubyAnim));
                        break;
                    }

                    glyphSpan = GlyphSpan.FromBounds(glyphSpan.Start, run.GlyphSpan.End);
                    i++;
                }

                flush();
            }

            GlyphRunRevealAnimation createAnim(GlyphSpan glyphSpan, float timePerGlyph)
                => new GlyphRunRevealAnimation(_textLayout, glyphSpan, timePerGlyph);
        }

        public bool Skipping { get; private set; }

        private uint NbNonWhitespaceGlyphs(GlyphSpan glyphSpan)
        {
            uint nbNonWhitespace = 0;
            foreach (ref readonly PositionedGlyph g in _textLayout.GetGlyphs(glyphSpan))
            {
                if (!g.IsWhitespace)
                {
                    nbNonWhitespace++;
                }
            }
            return nbNonWhitespace;
        }

        public override bool Update(float dt)
        {
            if (_anims.TryPeek(out AnimationPair animPair))
            {
                bool inProgress = animPair.Text.Update(dt);
                if (animPair.RubyText is object)
                {
                    inProgress |= animPair.RubyText.Update(dt);
                }
                if (!inProgress)
                {
                    _anims.Dequeue();
                }
            }

            return base.Update(dt);
        }

        public void Skip()
        {
            if (Skipping) { return; }
            var newQueue = new Queue<AnimationPair>();
            while (_anims.TryDequeue(out AnimationPair animPair))
            {
                var textAnim = (GlyphRunRevealAnimation)animPair.Text;
                GlyphRunRevealAnimation? rubyAnim = animPair.RubyText as GlyphRunRevealAnimation;
                var newTextAnim = new GlyphRunSkipAnimation(_textLayout, textAnim.RemainingGlyphs);
                GlyphRunSkipAnimation? newRubyAnim = rubyAnim is object
                    ? new GlyphRunSkipAnimation(_textLayout, rubyAnim.RemainingGlyphs)
                    : null;

                newQueue.Enqueue(new AnimationPair(newTextAnim, newRubyAnim));
            }

            _anims = newQueue;
            Skipping = true;
        }

        protected override AdvanceResult Advance()
        {
            return _anims.Count > 0
                ? AdvanceResult.KeepGoing
                : AdvanceResult.Stop;
        }
    }

    internal class GlyphRunRevealAnimation : Animation
    {
        private readonly TextLayout _textLayout;
        private readonly GlyphSpan _glyphSpan;
        private readonly float _timePerGlyph;

        private int _glyphsRevealed;
        private int _pos;

        public GlyphRunRevealAnimation(TextLayout textLayout, GlyphSpan glyphSpan, float timePerGlyph)
        {
            _textLayout = textLayout;
            _glyphSpan = glyphSpan;
            RemainingGlyphs = glyphSpan;
            _timePerGlyph = timePerGlyph;
            _pos = 0;
        }

        public GlyphSpan RemainingGlyphs { get; private set; }

        protected override AdvanceResult Advance()
        {
            int nbGlyphsToReveal = (int)((Elapsed / _timePerGlyph) - _glyphsRevealed);
            Span<float> opacity = _textLayout.GetOpacityValuesMut(_glyphSpan);
            while (nbGlyphsToReveal > 0 && _pos < _glyphSpan.Length)
            {
                if (!_textLayout.Glyphs[(int)_glyphSpan.Start + _pos].IsWhitespace)
                {
                    opacity[_pos] = 1.0f;
                    nbGlyphsToReveal--;
                    _glyphsRevealed++;
                }
                else
                {
                    // whitespace
                    opacity[_pos] = 1.0f;
                }
                _pos++;
            }

            if (_pos < _glyphSpan.Length)
            {
                opacity[_pos] = (Elapsed % _timePerGlyph) / _timePerGlyph;
            }

            RemainingGlyphs = new GlyphSpan(
                (uint)(_glyphSpan.Start + _pos),
                (uint)(_glyphSpan.Length - _pos)
            );
            return _pos == _glyphSpan.Length
                ? AdvanceResult.Stop
                : AdvanceResult.KeepGoing;
        }
    }

    internal class GlyphRunSkipAnimation : AnimationWithDuration
    {
        private readonly TextLayout _textLayout;
        private readonly GlyphSpan _firstGlyph;
        private readonly GlyphSpan _rest;

        private readonly float _initialFirstGlyphOpacity;
        private float _firstGlyphOpacity;
        private float _restOpacity;

        public GlyphRunSkipAnimation(TextLayout textLayout, GlyphSpan glyphSpan)
            : base(TimeSpan.FromMilliseconds(120))
        {
            _textLayout = textLayout;
            if (!glyphSpan.IsEmpty)
            {
                _firstGlyph = new GlyphSpan(glyphSpan.Start, 1);
                _rest = new GlyphSpan(glyphSpan.Start + 1, glyphSpan.Length - 1);
                _initialFirstGlyphOpacity = textLayout.OpacityValues[(int)glyphSpan.Start];
            }
        }

        protected override AdvanceResult Advance()
        {
            static void interpolate(ref float value, float start, float end, float factor)
            {
                float delta = end - start;
                value = start + delta * factor;
            }

            float factor = GetFactor(Progress, _easeFunction);
            interpolate(ref _firstGlyphOpacity, _initialFirstGlyphOpacity, 1.0f, factor);
            interpolate(ref _restOpacity, 0.0f, 1.0f, factor);

            _textLayout.GetOpacityValuesMut(_firstGlyph).Fill(_firstGlyphOpacity);
            _textLayout.GetOpacityValuesMut(_rest).Fill(_restOpacity);

            return base.Advance();
        }
    }

    internal sealed class TransitionAnimation : AnimationWithDuration, IDisposable
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

        protected override AdvanceResult Advance()
        {
            float delta = _dstFadeAmount - _srcFadeAmount;
            _fadeAmount = _srcFadeAmount + delta * GetFactor(Progress, _easeFunction);
            return base.Advance();
        }

        public void Dispose()
        {
            // TODO: ???
            //Mask.Dispose();
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
            float factor = GetFactor(Progress, _easeFunction);
            float delta = _endOpacity - _startOpacity;
            float current = _startOpacity + delta * factor;
            _entity.Color.SetAlpha(current);
            return base.Advance();
        }
    }

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

    internal sealed class BezierMoveAnimation : PropertyAnimation<RenderItem2D, Vector3>
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
