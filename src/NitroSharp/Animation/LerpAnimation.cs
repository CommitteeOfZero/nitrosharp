using System;
using NitroSharp.Experimental;
using NitroSharp.NsScript;

namespace NitroSharp.Animation
{
    internal abstract class LerpAnimation<T> : PropertyAnimation
        where T : unmanaged
    {
        protected LerpAnimation(
            Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None,
            bool repeat = false) : base(entity, duration, easingFunction, repeat)
        {
        }

        protected abstract EntityStorage.ComponentStorage<T> GetPropertyRow();

        protected override void Advance(float deltaMilliseconds)
        {
            InterpolateValue(ref GetPropertyRow().GetRef(Entity), CalculateFactor(Progress, NsEasingFunction));
        }

        protected abstract void InterpolateValue(ref T value, float factor);
    }
}
