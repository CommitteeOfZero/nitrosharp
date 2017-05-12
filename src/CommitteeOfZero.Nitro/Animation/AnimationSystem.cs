using MoeGame.Framework;
using System;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro.Animation
{
    public sealed class AnimationSystem : EntityProcessingSystem
    {
        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Animation));
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            foreach (Animation animation in entity.GetComponents<Animation>())
            {
                if (animation.IsEnabled)
                {
                    animation.Advance(deltaMilliseconds);
                }
            }
        }
    }
}
