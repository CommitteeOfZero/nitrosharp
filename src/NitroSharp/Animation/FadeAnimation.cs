using System;
using System.Numerics;
using NitroSharp.Animation;
using Veldrid;

namespace NitroSharp.Logic.Components
{
    internal sealed class FadeAnimation : LerpAnimation<RgbaFloat>
    {
        public FadeAnimation(Entity entity, TimeSpan duration,
            TimingFunction timingFunction = TimingFunction.Linear, bool repeat = false)
            : base(entity, duration, timingFunction, repeat)
        {
        }

        public float InitialOpacity;
        public float FinalOpacity;

        protected override EntityTable.Row<RgbaFloat> GetPropertyRow(World world)
        {
            var table = world.GetTable<RenderItemTable>(Entity);
            return table.Colors;
        }

        protected override void InterpolateValue(ref RgbaFloat value, float factor)
        {
            var channels = value.ToVector4();
            float delta = FinalOpacity - InitialOpacity;
            channels.W = InitialOpacity + delta * factor;
            value = new RgbaFloat(channels);
        }
    }
}
