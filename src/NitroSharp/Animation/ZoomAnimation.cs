using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;

namespace NitroSharp.Logic.Components
{
    internal sealed class ZoomAnimation : LerpAnimation<TransformComponents>
    {
        public ZoomAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        public Vector3 InitialScale;
        public Vector3 FinalScale;

        protected override EntityTable.Row<TransformComponents> GetPropertyRow(World world)
        {
            var table = world.GetTable<RenderItemTable>(Entity);
            return table.TransformComponents;
        }

        protected override void InterpolateValue(ref TransformComponents value, float factor)
        {
            Vector3 delta = FinalScale - InitialScale;
            value.Scale = InitialScale + delta * factor;
        }
    }
}
