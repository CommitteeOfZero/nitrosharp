﻿using HoppyFramework;
using System;

namespace ProjectHoppy.Graphics
{
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

        public event EventHandler Completed;

        public void RaiseCompleted()
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}
