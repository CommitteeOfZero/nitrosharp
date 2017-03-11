using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy.Graphics
{
    public class AnimationSystem : EntityProcessingSystem
    {
        public AnimationSystem()
            : base(typeof(FloatAnimation))
        {

        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            foreach (var animation in entity.GetComponents<FloatAnimation>())
            {
                if (animation.IsEnabled)
                {
                    animation.CurrentValue += animation.FinalValue * (deltaMilliseconds / (float)animation.Duration.TotalMilliseconds);
                    if (animation.CurrentValue >= animation.FinalValue)
                    {
                        animation.CurrentValue = animation.FinalValue;
                        animation.IsEnabled = false;
                    }

                    animation.PropertySetter(entity, animation.CurrentValue);
                }
            }
        }
    }
}
