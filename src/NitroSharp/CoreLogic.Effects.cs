using NitroSharp.NsScript;
using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        public override void Fade(string entityName, TimeSpan duration, NsRational opacity, NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                FadeCore(entity, duration, opacity, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }

        private static void FadeCore(Entity entity, TimeSpan duration, NsRational opacity, NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<FadeAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }

            float adjustedOpacity = opacity.Rebase(1.0f);
            var visual = entity.GetComponent<Visual2D>();
            if (visual != null)
            {
                if (duration > TimeSpan.Zero)
                {
                    var fn = (TimingFunction)easingFunction;
                    var animation = new FadeAnimation(visual, visual.Opacity, adjustedOpacity, duration, fn);
                    entity.AddComponent(animation);
                }
                else
                {
                    visual.Opacity = adjustedOpacity;
                }
            }
        }

        public override void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                MoveCore(entity, duration, x, y, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }

        private static void MoveCore(Entity entity, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<MoveAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }
            
            float targetX = x.Origin == NsCoordinateOrigin.CurrentValue ? entity.Transform.Position.X + x.Value : x.Value;
            float targetY = y.Origin == NsCoordinateOrigin.CurrentValue ? entity.Transform.Position.Y + y.Value : y.Value;
            Vector3 destination = new Vector3(targetX, targetY, 0);

            if (duration > TimeSpan.Zero)
            {
                var fn = (TimingFunction)easingFunction;
                var animation = new MoveAnimation(entity.Transform, entity.Transform.Position, destination, duration, fn);
                entity.AddComponent(animation);
            }
            else
            {
                entity.Transform.Position = destination;
            }
        }

        public override void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                ZoomCore(entity, duration, scaleX, scaleY, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }

        private static void ZoomCore(Entity entity, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<ZoomAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }
            
            scaleX = scaleX.Rebase(1.0f);
            scaleY = scaleY.Rebase(1.0f);

            if (duration > TimeSpan.Zero)
            {
                Vector3 initialScale = entity.Transform.Scale;
                Vector3 finalScale = new Vector3(scaleX, scaleY, 1);
                if (initialScale == finalScale)
                {
                    entity.Transform.Scale = Vector3.Zero;
                }

                var fn = (TimingFunction)easingFunction;
                var animation = new ZoomAnimation(entity.Transform, initialScale, finalScale, duration, fn);
                entity.AddComponent(animation);
            }
            else
            {
                entity.Transform.Scale = new Vector3(scaleX, scaleY, 1);
            }
        }

        public override void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity,
            NsRational finalOpacity, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay)
        {
            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            initialOpacity = initialOpacity.Rebase(1.0f);
            finalOpacity = finalOpacity.Rebase(1.0f);

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }
    }
}
