using MoeGame.Framework;
using System;

namespace CommitteeOfZero.Nitro.Animation
{
    public sealed class FloatAnimation : Animation
    {
        public Component TargetComponent { get; set; }
        public Action<Component, float> PropertySetter { get; set; }

        public float InitialValue { get; set; }
        public float FinalValue { get; set; }

        public override void Advance(float deltaMilliseconds)
        {
            float change = FinalValue - InitialValue;
            float newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);

            PropertySetter(TargetComponent, newValue);
            base.Advance(deltaMilliseconds);
        }
    }
}
