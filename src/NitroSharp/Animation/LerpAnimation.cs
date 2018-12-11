using System;

namespace NitroSharp.Animation
{
    internal abstract class LerpAnimation<T> : PropertyAnimation
        where T : unmanaged
    {
        protected LerpAnimation(
            Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear,
            bool repeat = false) : base(entity, duration, timingFunction, repeat)
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
            InterpolateValue(ref PropertyRow.Mutate(Entity), CalculateFactor(Progress, TimingFunction));
        }

        protected abstract void InterpolateValue(ref T value, float factor);
    }
}
