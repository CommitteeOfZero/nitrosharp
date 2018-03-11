using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal class Vector3Animation : AnimationBase
    {
        public Vector3Animation(Component transform, Action<Component, Vector3> propertySetter, Vector3 initialValue,
            Vector3 finalValue, TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(duration, timingFunction)
        {
            Transform = transform;
            PropertySetter = propertySetter;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public Component Transform { get; }
        public Action<Component, Vector3> PropertySetter { get; }

        public Vector3 InitialValue { get; }
        public Vector3 FinalValue { get; }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            Vector3 change = FinalValue - InitialValue;
            Vector3 newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);
            PropertySetter(Transform, newValue);

            if (HasCompleted)
            {
                RaiseCompleted();
            }
        }
    }
}
