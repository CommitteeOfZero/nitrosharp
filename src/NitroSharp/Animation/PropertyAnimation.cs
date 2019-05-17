using System;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Utilities;

namespace NitroSharp.Animation
{
    internal abstract class PropertyAnimation
    {
        private float _elapsed;
        private bool _initialized;
        private bool _completed;

        protected PropertyAnimation(
            Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None,
            bool repeat = false)
        {
            Entity = entity;
            Duration = duration;
            NsEasingFunction = easingFunction;
            Repeat = repeat;
        }

        public Entity Entity { get; }
        public TimeSpan Duration { get; protected set; }
        public NsEasingFunction NsEasingFunction { get; }
        public bool Repeat { get; }

        public bool IsBlocking { get; set; }
        public ThreadContext WaitingThread { get; set; }

        public event Action Completed;

        protected float Elapsed => _elapsed;
        public float Progress => MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool HasCompleted => _elapsed >= Duration.TotalMilliseconds;

        public bool Update(World world, float deltaMilliseconds)
        {
            if (_initialized)
            {
                _elapsed += deltaMilliseconds;
            }
            else
            {
                Setup(world);
                _initialized = true;
            }

            if (!_completed)
            {
                Advance(deltaMilliseconds);
                PostAdvance(world);
                return !_completed;
            }

            return false;
        }

        protected virtual void Setup(World world)
        {
        }

        protected virtual void Advance(float deltaMilliseconds)
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
                    world.DeactivateAnimation(this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float CalculateProgress(float elapsed, float duration)
            => MathUtil.Clamp(elapsed / duration, 0.0f, 1.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float CalculateFactor(float progress, NsEasingFunction function)
        {
            switch (function)
            {
                case NsEasingFunction.QuadraticEaseIn:
                    return (float)Math.Pow(progress, 2);
                case NsEasingFunction.CubicEaseIn:
                    return (float)Math.Pow(progress, 3);
                case NsEasingFunction.QuarticEaseIn:
                    return (float)Math.Pow(progress, 4);
                case NsEasingFunction.QuadraticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 2);
                case NsEasingFunction.CubicEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 3);
                case NsEasingFunction.QuarticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 4);
                case NsEasingFunction.SineEaseIn:
                    return 1.0f - (float)Math.Cos(progress * Math.PI * 0.5f);
                case NsEasingFunction.SineEaseOut:
                    return (float)Math.Sin(progress * Math.PI * 0.5f);
                case NsEasingFunction.SineEaseInOut:
                    return 0.5f * (1.0f - (float)Math.Cos(progress * Math.PI));
                case NsEasingFunction.SineEaseOutIn:
                    return (float)(Math.Acos(1.0f - progress * 2.0f) / Math.PI);
                case NsEasingFunction.None:
                default:
                    return progress;
            }
        }
    }
}
