using ProjectHoppy.Framework;
using System;

namespace ProjectHoppy.Graphics
{
    public class FloatAnimation : Component
    {
        public Component TargetComponent { get; set; }
        public Action<Component, float> PropertySetter { get; set; }
        public float InitialValue { get; set; }
        public float FinalValue { get; set; }
        public float CurrentValue { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
