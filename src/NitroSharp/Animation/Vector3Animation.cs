using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal class Vector3Animation<TComponent> : ComponentPropertyAnimation<TComponent, Vector3>
        where TComponent : Component
    {
        public Vector3Animation(
            TComponent targetComponent, Action<TComponent, Vector3> propertySetter,
            Vector3 initialValue, Vector3 finalValue, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(targetComponent, propertySetter, initialValue, finalValue, duration, timingFunction, repeat)
        {
        }

        protected override Vector3 InterpolateValue(float factor)
        {
            var change = FinalValue - InitialValue;
            var newValue = InitialValue + change * factor;
            return newValue;
        }
    }
}
