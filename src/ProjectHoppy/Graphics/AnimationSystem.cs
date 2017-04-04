using HoppyFramework;
using System;

namespace ProjectHoppy.Graphics
{
    public class AnimationSystem : EntityProcessingSystem
    {
        public AnimationSystem() : base(typeof(FloatAnimation))
        {
            EntityAdded += OnEntityAdded;
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var animation = e.GetComponent<FloatAnimation>();
            animation.PropertySetter(animation.TargetComponent, animation.InitialValue);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            foreach (var animation in entity.GetComponents<FloatAnimation>())
            {
                if (animation.IsEnabled)
                {
                    float currentValue = animation.PropertyGetter(animation.TargetComponent);
                    bool increasing = animation.FinalValue > animation.InitialValue;

                    float change = animation.FinalValue - animation.InitialValue;
                    float progress = animation.Elapsed / (float)animation.Duration.TotalMilliseconds;
                    currentValue = change * Factor(progress, animation.TimingFunction);

                    if (increasing && currentValue >= animation.FinalValue || !increasing && currentValue <= animation.FinalValue)
                    {
                        currentValue = animation.FinalValue;
                        animation.IsEnabled = false;
                    }

                    animation.PropertySetter(animation.TargetComponent, currentValue);
                    animation.Elapsed += deltaMilliseconds;
                }
            }
        }

        private static float Factor(float progress, TimingFunction function)
        {
            switch (function)
            {
                case TimingFunction.QuadraticEaseIn:
                    return (float)Math.Pow(progress, 2);

                case TimingFunction.CubicEaseIn:
                    return (float)Math.Pow(progress, 3);

                case TimingFunction.QuarticEaseIn:
                    return (float)Math.Pow(progress, 4);

                case TimingFunction.QuadraticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 2);

                case TimingFunction.CubicEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 3);

                case TimingFunction.QuarticEaseOut:
                    return 1.0f - (float)Math.Pow(1.0f - progress, 4);

                case TimingFunction.Linear:
                default:
                    return progress;
            }
        }
    }
}
