using System;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.NsScript;

namespace NitroSharp.Animation
{
    internal sealed class TransitionAnimation : LerpAnimation<Material>
    {
        public TransitionAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
        {
        }

        public float InitialFadeAmount;
        public float FinalFadeAmount;

        protected override EntityStorage.ComponentStorage<Material> GetPropertyRow()
        {
            var storage = World.GetStorage<RenderItemStorage>(Entity);
            return storage.Materials;
        }

        protected override void InterpolateValue(ref Material value, float factor)
        {
            float delta = FinalFadeAmount - InitialFadeAmount;
            value.TransitionParameters.FadeAmount = InitialFadeAmount + delta * factor;
        }
    }
}
