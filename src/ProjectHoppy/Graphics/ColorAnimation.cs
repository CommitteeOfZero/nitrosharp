using HoppyFramework;
using System;

namespace ProjectHoppy.Graphics
{
    public class ColorAnimation : Component
    {
        public Action<Entity, RgbaValueF> PropertySetter { get; set; }
        public RgbaValueF InitialValue { get; set; }
        public RgbaValueF FinalValue { get; set; }
        public RgbaValueF CurrentValue { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Repeat { get; set; }
    }
}
