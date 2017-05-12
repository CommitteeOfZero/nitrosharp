using MoeGame.Framework;
using System;

namespace CommitteeOfZero.Nitro.Animation
{
    public abstract class Animation : Component
    {
        protected float _elapsed;

        public TimeSpan Duration { get; set; }
        public TimingFunction TimingFunction { get; set; }
        public float Progress => SharpDX.MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);

        public event EventHandler Completed;

        public virtual void Advance(float deltaMilliseconds)
        {
            _elapsed += deltaMilliseconds;
            if (Progress == 1.0f)
            {
                Completed?.Invoke(this, EventArgs.Empty);
                Entity.RemoveComponent(this);
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

                case TimingFunction.Linear:
                default:
                    return progress;
            }
        }
    }
}
