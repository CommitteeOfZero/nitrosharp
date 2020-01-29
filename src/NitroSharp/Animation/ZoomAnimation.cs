using System;
using System.Numerics;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.NsScript;

namespace NitroSharp.Animation
{
    internal sealed class ZoomAnimation : LerpAnimation<TransformComponents>
    {
        public Vector3 InitialScale;
        public Vector3 FinalScale;


        public ZoomAnimation(Entity entity, TimeSpan duration,
            NsEasingFunction easingFunction = NsEasingFunction.None, bool repeat = false)
            : base(entity, duration, easingFunction, repeat)
        {
        }

        protected override EntityStorage.ComponentVec<TransformComponents> SelectComponentVec()
        {
            var storage = World.GetStorage<RenderItemStorage>(Entity);
            return storage.TransformComponents;
        }

        protected override void InterpolateValue(ref TransformComponents value, float factor)
        {
            Vector3 delta = FinalScale - InitialScale;
            value.Scale = InitialScale + delta * factor;
        }
    }
}
