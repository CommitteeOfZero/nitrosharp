using System;
using NitroSharp.Animation;

namespace NitroSharp.Logic.Components
{
    internal sealed class FadeAnimation : LerpAnimation<float>
    {
        public FadeAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        public float InitialOpacity;
        public float FinalOpacity;

        protected override ref float GetReference(World world)
            => ref world.GetTable<Visuals>(Entity).Colors.Mutate(Entity)._channels.W;

        protected override float InterpolateValue(float factor)
        {
            float delta = FinalOpacity - InitialOpacity;
            return InitialOpacity + delta * factor;
        }
    }
}
