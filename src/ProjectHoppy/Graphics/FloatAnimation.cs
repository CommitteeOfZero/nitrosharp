using HoppyFramework;
using System;

namespace ProjectHoppy.Graphics
{
    public enum TimingFunction
    {
        Linear = 0,

        QuadraticEaseIn,
        CubicEaseIn,
        QuarticEaseIn,

        QuadraticEaseOut,
        CubicEaseOut,
        QuarticEaseOut
    }

    public class FloatAnimation : Component
    {
        public Component TargetComponent { get; set; }
        public Func<Component, float> PropertyGetter { get; set; }
        public Action<Component, float> PropertySetter { get; set; }

        public float InitialValue { get; set; }
        public float FinalValue { get; set; }
        public TimeSpan Duration { get; set; }
        public TimingFunction TimingFunction { get; set; }

        public float Elapsed { get; set; }
    }
}
