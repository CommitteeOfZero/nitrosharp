using System;
using NitroSharp.NsScript;

namespace NitroSharp.Animation
{
    internal abstract class LerpAnimation<T> : PropertyAnimation
        where T : unmanaged
    {
        protected LerpAnimation(
            Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None,
            bool repeat = false) : base(entity, duration, easingFunction, repeat)
        {
        }

        protected EntityTable.Row<T> PropertyRow { get; private set; }

        protected override void Setup(World world)
        {
            PropertyRow = GetPropertyRow(world);
        }

        protected abstract EntityTable.Row<T> GetPropertyRow(World world);

        protected override void Advance(float deltaMilliseconds)
        {
            InterpolateValue(ref PropertyRow.Mutate(Entity), CalculateFactor(Progress, NsEasingFunction));
        }

        protected abstract void InterpolateValue(ref T value, float factor);
    }
}
