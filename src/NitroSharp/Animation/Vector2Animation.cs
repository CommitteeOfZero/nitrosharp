using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal class Vector2Animation : AnimationBase
    {
        public Vector2Animation(
            Component transform, Action<Component, Vector2> propertySetter,
            Vector2 initialValue, Vector2 finalValue, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear)
            : base(duration, timingFunction)
        {
            Transform = transform;
            PropertySetter = propertySetter;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public Component Transform { get; }
        public Action<Component, Vector2> PropertySetter { get; }

        public Vector2 InitialValue { get; }
        public Vector2 FinalValue { get; }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            Vector2 change = FinalValue - InitialValue;
            Vector2 newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);
            PropertySetter(Transform, newValue);

            if (HasCompleted)
            {
                RaiseCompleted();
            }
        }
    }
}
