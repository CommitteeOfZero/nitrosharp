using System;
using System.Numerics;

namespace MoeGame.Framework.Animation
{
    public sealed class Vector2Animation : AnimationBase
    {
        public Vector2Animation(Component targetComponent, Action<Component, Vector2> propertySetter,
            Vector2 initialValue, Vector2 finalValue, TimeSpan duration, TimingFunction timingFunction)
            : base(duration, timingFunction)
        {
            TargetComponent = targetComponent;
            PropertySetter = propertySetter;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public Vector2Animation(Component targetComponent, Action<Component, Vector2> propertySetter,
            Vector2 initialValue, Vector2 finalValue, TimeSpan duration)
            : this(targetComponent, propertySetter, initialValue, finalValue, duration, TimingFunction.Linear)
        {
        }

        public Component TargetComponent { get; }
        public Action<Component, Vector2> PropertySetter { get; }

        public Vector2 InitialValue { get; }
        public Vector2 FinalValue { get; }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            Vector2 change = FinalValue - InitialValue;
            Vector2 newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);
            PropertySetter(TargetComponent, newValue);
        }
    }
}
