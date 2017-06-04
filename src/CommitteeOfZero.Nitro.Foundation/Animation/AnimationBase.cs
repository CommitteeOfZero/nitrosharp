using System;

namespace CommitteeOfZero.Nitro.Foundation.Animation
{
    public abstract class AnimationBase : Component
    {
        protected float _elapsed;

        protected AnimationBase()
        {
        }

        protected AnimationBase(TimeSpan duration)
            : this(duration, TimingFunction.Linear)
        {
        }

        protected AnimationBase(TimeSpan duration, TimingFunction timingFunction)
        {
            Duration = duration;
            TimingFunction = TimingFunction;
        }

        public TimeSpan Duration { get; protected set; }
        public TimingFunction TimingFunction { get; protected set; }
        public float Progress => SharpDX.MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);
        public bool Started { get; private set; }

        protected bool LastFrame => Progress == 1.0f;

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

                case TimingFunction.Linear:
                default:
                    return progress;
            }
        }
    }
}
