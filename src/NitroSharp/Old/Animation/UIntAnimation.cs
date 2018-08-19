using System;

namespace NitroSharp.Animation
{
    internal class UIntAnimation<TComponent> : ComponentPropertyAnimation<TComponent, uint>
        where TComponent : Component
    {
        public UIntAnimation(
            TComponent targetComponent, Action<TComponent, uint> propertySetter,
            uint initialValue, uint finalValue, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(targetComponent, propertySetter, initialValue, finalValue, duration, timingFunction, repeat)
        {
        }

        protected override uint InterpolateValue(float factor)
        {
            var change = FinalValue - InitialValue;
            var newValue = InitialValue + change * factor;
            return (uint)newValue;
        }
    }
}
