using System;
using System.Numerics;
using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal sealed class ZoomAnimation : LerpAnimation<Vector3>
    {
        public ZoomAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        public Vector3 InitialScale;
        public Vector3 FinalScale;

        protected override ref Vector3 GetReference(World world)
           => ref world.GetTable<Visuals>(Entity).TransformComponents.Mutate(Entity).Scale;

        protected override Vector3 InterpolateValue(float factor)
        {
            Vector3 delta = FinalScale - InitialScale;
            return InitialScale + delta * factor;
        }
    }
}
