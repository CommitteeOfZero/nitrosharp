using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class RotateAnimation : AnimationBase
    {
        private readonly Transform _target;

        public RotateAnimation(
            Transform transform, Quaternion initialValue, Quaternion finalValue,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(duration, timingFunction)
        {
            _target = transform;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public Quaternion InitialValue { get; }
        public Quaternion FinalValue { get; }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            //_target.Rotation = Quaternion.Lerp(InitialValue, FinalValue, CalculateFactor(Progress, TimingFunction));

            if (HasCompleted)
            {
                RaiseCompleted();
            }
        }
    }
}
