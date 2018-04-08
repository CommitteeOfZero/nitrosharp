using System;
using NitroSharp.Utilities;

namespace NitroSharp.Animation
{
    internal abstract class AnimationBase : Component
    {
        private float _elapsed;
        private bool _initialized;

        protected AnimationBase(TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
        {
            Duration = duration;
            TimingFunction = timingFunction;
            Repeat = repeat;
        }

        public TimeSpan Duration { get; }
        public TimingFunction TimingFunction { get; }
        public float Progress => MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool Repeat { get; }

        public event EventHandler Completed;

        public virtual void Advance(float deltaMilliseconds)
        {
            if (_initialized)
            {
                _elapsed += deltaMilliseconds;
            }
            else
            {
                _initialized = true;
            }
        }

        protected void PostAdvance()
        {
            if (Progress == 1.0f)
            {
                if (Repeat)
                {
                    _elapsed = 0;
                    _initialized = false;
                }
                else
                {
                    Completed?.Invoke(this, EventArgs.Empty);
                    Entity.RemoveComponent(this);
                }
            }
        }

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
