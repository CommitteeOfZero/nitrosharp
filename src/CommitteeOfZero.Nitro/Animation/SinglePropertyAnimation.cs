using System;

namespace CommitteeOfZero.Nitro.Animation
{
    public abstract class SinglePropertyAnimation : Animation
    {
        public TimeSpan Duration { get; set; }
        public TimingFunction TimingFunction { get; set; }
        public float Progress => SharpDX.MathUtil.Clamp(_elapsed / (float)Duration.TotalMilliseconds, 0.0f, 1.0f);

        public override event EventHandler Completed;

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

        public override void Advance(float deltaMilliseconds)
        {
            _elapsed += deltaMilliseconds;
            if (Progress == 1.0f)
            {
                Completed?.Invoke(this, EventArgs.Empty);
                IsEnabled = false;
            }
        }
    }
}
