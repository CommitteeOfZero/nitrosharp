using MoeGame.Framework;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Animation
{
    public class Vector2Animation : Animation
    {
        public Component TargetComponent { get; set; }
        public Action<Component, Vector2> PropertySetter { get; set; }

        public Vector2 InitialValue { get; set; }
        public Vector2 FinalValue { get; set; }

        public override void Advance(float deltaMilliseconds)
        {
            Vector2 change = FinalValue - InitialValue;
            Vector2 newValue = InitialValue + change * CalculateFactor(Progress, TimingFunction);

            PropertySetter(TargetComponent, newValue);
            base.Advance(deltaMilliseconds);
        }
    }
}
