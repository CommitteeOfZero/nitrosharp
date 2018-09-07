using System;
using System.Collections.Generic;

namespace NitroSharp.Animation
{
    internal sealed class OldAnimationSystem : OldEntityProcessingSystem
    {
        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(OldAnimationBase));
        }

        public override void Process(OldEntity entity, float deltaMilliseconds)
        {
            foreach (OldAnimationBase animation in entity.GetComponents<OldAnimationBase>())
            {
                if (animation.IsEnabled)
                {
                    animation.Advance(deltaMilliseconds);
                }
            }
        }
    }
}
