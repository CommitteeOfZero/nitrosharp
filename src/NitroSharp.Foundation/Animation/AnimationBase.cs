using System;

namespace NitroSharp.Foundation.Animation
{
    public abstract class AnimationBase : Component
    {
        protected float _elapsed;

        protected AnimationBase()
        {
        }

        protected AnimationBase(TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
        {
            Duration = duration;
            TimingFunction = timingFunction;
        }

        public TimeSpan Duration { get; protected set; }
        public TimingFunction TimingFunction { get; }
        public float Progress => SharpDX.MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool Started { get; private set; }
        public bool HasCompleted => Progress == 1.0f;

        public event EventHandler Completed;

        public virtual void Advance(float deltaMilliseconds)
        {
            Started = true;
            _elapsed += deltaMilliseconds;
        }

        protected void RaiseCompleted()
        {
            Completed?.Invoke(this, EventArgs.Empty);
            Entity.RemoveComponent(this);
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
