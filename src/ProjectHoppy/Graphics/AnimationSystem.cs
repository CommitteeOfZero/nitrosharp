using HoppyFramework;
using System;
using System.Diagnostics;
using System.Numerics;

namespace ProjectHoppy.Graphics
{
    public class AnimationSystem : EntityProcessingSystem
    {
        public AnimationSystem() : base(typeof(FloatAnimation), typeof(Vector2Animation))
        {
            EntityAdded += OnEntityAdded;
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var floatAnimation = e.GetComponent<FloatAnimation>();
            floatAnimation?.PropertySetter(floatAnimation.TargetComponent, floatAnimation.InitialValue);

            var vector2Animation = e.GetComponent<Vector2Animation>();
            vector2Animation?.PropertySetter(vector2Animation.TargetComponent, vector2Animation.InitialValue);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            foreach (var animation in entity.GetComponents<FloatAnimation>())
            {
                if (animation.IsEnabled)
                {
                    float newValue = AdvanceAnimation(animation.InitialValue, animation.FinalValue, animation.Elapsed, animation.Duration, animation.TimingFunction);

                    animation.PropertySetter(animation.TargetComponent, newValue);
                    animation.Elapsed += deltaMilliseconds;

                    if (newValue == animation.FinalValue)
                    {
                        animation.IsEnabled = false;
                        animation.RaiseCompleted();
                    }
                }
            }

            foreach (var animation in entity.GetComponents<Vector2Animation>())
            {
                if (animation.IsEnabled)
                {
                    Vector2 newValue = AdvanceAnimation(animation.InitialValue, animation.FinalValue, animation.Elapsed, animation.Duration, animation.TimingFunction);

                    Debug.WriteLine(newValue);

                    animation.PropertySetter(animation.TargetComponent, newValue);
                    animation.Elapsed += deltaMilliseconds;

                    if (newValue == animation.FinalValue)
                    {
                        animation.IsEnabled = false;
                        animation.RaiseCompleted();
                    }
                }
            }
        }

        private static float AdvanceAnimation(float initialValue, float finalValue, float elapsed, TimeSpan duration, TimingFunction timingFunction)
        {
            float change = finalValue - initialValue;
            float progress = SharpDX.MathUtil.Clamp(elapsed / (float)duration.TotalMilliseconds, 0.0f, 1.0f);
            return initialValue + change * Factor(progress, timingFunction);
        }

        private static Vector2 AdvanceAnimation(Vector2 initialValue, Vector2 finalValue, float elapsed, TimeSpan duration, TimingFunction timingFunction)
        {
            Vector2 change = finalValue - initialValue;
            float progress = SharpDX.MathUtil.Clamp(elapsed / (float)duration.TotalMilliseconds, 0.0f, 1.0f);
            return initialValue + change * Factor(progress, timingFunction);
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
