using System;

namespace ProjectHoppy.Graphics
{
    public class FloatAnimation : Component
    {
        public Action<Entity, float> PropertySetter { get; set; }
        public float CurrentValue { get; set; }
        public float FinalValue { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
