using MoeGame.Framework;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class Vector2Animation : Component
    {
        public Component TargetComponent { get; set; }
        public Func<Component, Vector2> PropertyGetter { get; set; }
        public Action<Component, Vector2> PropertySetter { get; set; }

        public Vector2 InitialValue { get; set; }
        public Vector2 FinalValue { get; set; }
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
