using System;
using System.Numerics;
using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal sealed class MoveAnimation : LerpAnimation<Vector3>
    {
        public Vector3 StartPosition;
        public Vector3 Destination;

        public MoveAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        protected override ref Vector3 GetReference(World world)
            => ref world.GetTable<RenderItemTable>(Entity).TransformComponents.Mutate(Entity).Position;

        protected override Vector3 InterpolateValue(float factor)
        {
            Vector3 delta = Destination - StartPosition;
            return StartPosition + delta * factor;
        }
    }
}
