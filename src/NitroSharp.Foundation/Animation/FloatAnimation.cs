using System;

namespace NitroSharp.Foundation.Animation
{
    public sealed class FloatAnimation : AnimationBase
    {
        public FloatAnimation(Component targetComponent, Action<Component, float> propertySetter,
            float initialValue, float finalValue, TimeSpan duration, TimingFunction timingFunction)
            : base(duration, timingFunction)
        {
            TargetComponent = targetComponent;
            PropertySetter = propertySetter;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public FloatAnimation(Component targetComponent, Action<Component, float> propertySetter,
            float initialValue, float finalValue, TimeSpan duration)
            : this(targetComponent, propertySetter, initialValue, finalValue, duration, TimingFunction.Linear)
        {
        }

        public Component TargetComponent { get; }
        public Action<Component, float> PropertySetter { get; }

        public float InitialValue { get; }
        public float FinalValue { get; }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            float change = FinalValue - InitialValue;
            float newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);
            PropertySetter(TargetComponent, newValue);

            if (LastFrame)
            {
                RaiseCompleted();
            }
        }
    }
}
