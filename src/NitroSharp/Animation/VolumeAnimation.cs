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

        protected override EntityTable.Row<float> GetPropertyRow(World world)
        {
            var table = world.GetTable<AudioClipTable>(Entity);
            return table.Volume;
        }

        protected override void InterpolateValue(ref float value, float factor)
        {
            float delta = FinalVolume - InitialVolume;
            value = InitialVolume + delta * factor;
        }
    }
}
