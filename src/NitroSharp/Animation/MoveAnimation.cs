using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;
using NitroSharp.NsScript;

namespace NitroSharp.Logic.Components
{
    internal sealed class MoveAnimation : LerpAnimation<TransformComponents>
    {
        public Vector3 StartPosition;
        public Vector3 Destination;

        public MoveAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
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
