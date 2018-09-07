using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Utilities;

namespace NitroSharp.Animation
{
    internal abstract class AnimationBase : AttachedBehavior
    {
        private float _elapsed;
        private bool _initialized;
        private bool _completed;

        protected AnimationBase(
            Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear,
            bool repeat = false) : base(entity)
        {
            Duration = duration;
            TimingFunction = timingFunction;
            Repeat = repeat;
        }

        public TimeSpan Duration { get; protected set; }
        public TimingFunction TimingFunction { get; }
        public bool Repeat { get; }

        public event Action Completed;

        protected float Elapsed => _elapsed;
        public float Progress => MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool HasCompleted => _elapsed >= Duration.TotalMilliseconds;

        public override void Update(World world, float deltaMilliseconds)
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
                Advance(world, deltaMilliseconds);
                PostAdvance(world);
            }
        }

        protected virtual void Advance(World world, float deltaMilliseconds)
        {
        }

        private void PostAdvance(World world)
        {
            if (HasCompleted)
            {
                if (Repeat)
                {
                    _elapsed = 0;
                    _initialized = false;
                }
                else
                {
                    _completed = true;
                    Completed?.Invoke();
                    world.DeactivateBehavior(this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float CalculateProgress(float elapsed, float duration)
            => MathUtil.Clamp(elapsed / duration, 0.0f, 1.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float CalculateFactor(float progress, TimingFunction function)
        {
            switch (function)
            {
                case TimingFunction.QuadraticEaseIn:
                    return (float)Math.Pow(progress, 2);

                case TimingFunction.CubicEaseIn:
                    return (float)Math.Pow(progress, 3);

                case TimingFunction.QuarticEaseIn:
                    return (float)Math.Pow(progress, 4);

                case TimingFunction.QuadraticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 2);

                case TimingFunction.CubicEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 3);

                case TimingFunction.QuarticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 4);

                case TimingFunction.SineEaseIn:
                    return 1.0f - (float)Math.Cos(progress * Math.PI * 0.5f);

                case TimingFunction.SineEaseOut:
                    return (float)Math.Sin(progress * Math.PI * 0.5f);

                case TimingFunction.SineEaseInOut:
                    return 0.5f * (1.0f - (float)Math.Cos(progress * Math.PI));

                case TimingFunction.SineEaseOutIn:
                    return (float)(Math.Acos(1.0f - progress * 2.0f) / Math.PI);

                case TimingFunction.Linear:
                default:
                    return progress;
            }
        }
    }
}
