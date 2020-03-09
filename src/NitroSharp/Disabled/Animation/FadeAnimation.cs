using System;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Old;
using NitroSharp.NsScript;
using Veldrid;

namespace NitroSharp.Animation
{
    internal sealed class FadeAnimation : LerpAnimation<Material>
    {
        public FadeAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
        {
        }

        public float InitialOpacity;
        public float FinalOpacity;

        protected override void Setup(World world)
        {
            world.EnableEntity(Entity);
            base.Setup(world);
        }

        protected override void OnCompleted(World world)
        {
            if (FinalOpacity < 0.01f)
            {
                world.ScheduleDisableEntity(Entity);
            }
        }

        protected override EntityStorage.ComponentVec<Material> SelectComponentVec()
        {
            var storage = World.GetStorage<RenderItemStorage>(Entity);
            return storage.Materials;
        }

        protected override void InterpolateValue(ref Material value, float factor)
        {
            var channels = value.Color.ToVector4();
            float delta = FinalOpacity - InitialOpacity;
            channels.W = InitialOpacity + delta * factor;
            value.Color = new RgbaFloat(channels);
        }
    }
}
