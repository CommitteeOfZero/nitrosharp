using System;

namespace NitroSharp.Animation
{
    internal abstract class ComponentPropertyAnimation<TComponent, TProperty> : AnimationBase
        where TComponent : Component
        where TProperty : struct
    {
        private readonly TComponent _component;
        private readonly Action<TComponent, TProperty> _propertySetter;

        public TProperty InitialValue { get; }
        public TProperty FinalValue { get; }

        public ComponentPropertyAnimation(
            TComponent targetComponent, Action<TComponent, TProperty> propertySetter,
            TProperty initialValue, TProperty finalValue, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(duration, timingFunction, repeat)
        {
            _component = targetComponent;
            _propertySetter = propertySetter;
            InitialValue = initialValue;
            FinalValue = finalValue;
        }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            var newValue = InterpolateValue(CalculateFactor(Progress, TimingFunction));
            _propertySetter(_component, newValue);
            
            PostAdvance();
        }

        protected abstract TProperty InterpolateValue(float factor);
    }
}
