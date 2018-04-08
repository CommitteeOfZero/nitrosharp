using System;

namespace NitroSharp.Animation
{
    internal class FloatAnimation<TComponent> : ComponentPropertyAnimation<TComponent, float>
        where TComponent : Component
    {
        public FloatAnimation(
            TComponent targetComponent, Action<TComponent, float> propertySetter,
            float initialValue, float finalValue, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(targetComponent, propertySetter, initialValue, finalValue, duration, timingFunction, repeat)
        {
        }

        protected override float InterpolateValue(float factor)
        {
            var change = FinalValue - InitialValue;
            var newValue = InitialValue + change * factor;
            return newValue;
        }
    }
}
