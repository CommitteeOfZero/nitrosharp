using System;

namespace NitroSharp.Animation
{
    internal sealed class VolumeAnimation : LerpAnimation<float>
    {
        public VolumeAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear,
            bool repeat = false) : base(entity, duration, timingFunction, repeat)
        {
        }

        public float InitialVolume;
        public float FinalVolume;

        protected override ref float GetReference(World world)
            => ref world.AudioClips.Volume.Mutate(Entity);

        protected override float InterpolateValue(float factor)
        {
            float delta = FinalVolume - InitialVolume;
            return InitialVolume + delta * factor;
        }
    }
}
