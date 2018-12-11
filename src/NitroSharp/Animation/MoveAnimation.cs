using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;

namespace NitroSharp.Logic.Components
{
    internal sealed class MoveAnimation : LerpAnimation<TransformComponents>
    {
        public Vector3 StartPosition;
        public Vector3 Destination;

        public MoveAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        protected override EntityTable.Row<TransformComponents> GetPropertyRow(World world)
        {
            var table = world.GetTable<RenderItemTable>(Entity);
            return table.TransformComponents;
        }

        protected override void InterpolateValue(ref TransformComponents value, float factor)
        {
            Vector3 delta = Destination - StartPosition;
            value.Position = StartPosition + delta * factor;
        }
    }
}
