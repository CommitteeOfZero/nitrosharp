using System;

namespace NitroSharp.Animation
{
    internal abstract class LerpAnimation<T> : PropertyAnimation where T : unmanaged
    {
        protected LerpAnimation(
            Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear,
            bool repeat = false) : base(entity, duration, timingFunction, repeat)
        {
        }

        protected override void Advance(World world, float deltaMilliseconds)
        {
            T newValue = InterpolateValue(CalculateFactor(Progress, TimingFunction));
            GetReference(world) = newValue;
        }

        protected abstract ref T GetReference(World world);
        protected abstract T InterpolateValue(float factor);
    }
}
