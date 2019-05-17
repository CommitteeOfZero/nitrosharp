using System;
using NitroSharp.NsScript;

namespace NitroSharp.Animation
{
    internal sealed class VolumeAnimation : LerpAnimation<float>
    {
        public VolumeAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None,
            bool repeat = false) : base(entity, duration, easingFunction, repeat)
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
