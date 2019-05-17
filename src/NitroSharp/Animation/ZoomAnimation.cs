using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;
using NitroSharp.NsScript;

namespace NitroSharp.Logic.Components
{
    internal sealed class ZoomAnimation : LerpAnimation<TransformComponents>
    {
        public ZoomAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
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
