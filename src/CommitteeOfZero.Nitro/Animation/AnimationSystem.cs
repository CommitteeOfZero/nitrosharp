using MoeGame.Framework;
using System;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro.Animation
{
    public class AnimationSystem : EntityProcessingSystem
    {
        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(FloatAnimation));
            interests.Add(typeof(Vector2Animation));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var animation = entity.GetComponent<Animation>();
            animation.SetInitialValue();
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            foreach (var animationType in this.Interests)
            {
                foreach (Animation animation in entity.GetComponents(animationType))
                {
                    animation.Advance(deltaMilliseconds);
                }
            }
        }
    }
}
