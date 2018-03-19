using NitroSharp.NsScript;
using System;
using System.Numerics;
using NitroSharp.Animation;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        public override void Fade(
            string entityName, TimeSpan duration, NsRational opacity,
            NsEasingFunction easingFunction, TimeSpan delay)
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

        private static void FadeCore(
            Entity entity, TimeSpan duration, NsRational dstOpacity, NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<FadeAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }

            float adjustedOpacity = dstOpacity.Rebase(1.0f);
            var visual = entity.Visual;
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

        public override void Move(
            string entityName, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                MoveCore(entity, duration, dstX, dstY, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }

        private static void MoveCore(
            Entity entity, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<MoveAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }

            var transform = entity.Transform;
            ref var position = ref transform.Position;

            float targetX = dstX.Origin == NsCoordinateOrigin.CurrentValue
                ? position.X + dstX.Value
                : dstX.Value;

            float targetY = dstY.Origin == NsCoordinateOrigin.CurrentValue
                ? position.Y + dstY.Value
                : dstY.Value;

            var destination = new Vector3(targetX, targetY, 0);
            if (duration > TimeSpan.Zero)
            {
                var fn = (TimingFunction)easingFunction;
                var animation = new MoveAnimation(transform, position, destination, duration, fn);
                entity.AddComponent(animation);
            }
            else
            {
                position = destination;
            }
        }

        public override void Zoom(
            string entityName, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                ZoomCore(entity, duration, dstScaleX, dstScaleY, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, delay);
            }
        }

        private static void ZoomCore(
            Entity entity, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction)
        {
            var existingAnimation = entity.GetComponent<ZoomAnimation>();
            if (existingAnimation != null)
            {
                entity.RemoveComponent(existingAnimation);
            }

            var transform = entity.Transform;
            ref var scale = ref transform.Scale;

            dstScaleX = dstScaleX.Rebase(1.0f);
            dstScaleY = dstScaleY.Rebase(1.0f);

            if (duration > TimeSpan.Zero)
            {
                Vector3 finalScale = new Vector3(dstScaleX, dstScaleY, 1);
                if (scale == finalScale)
                {
                    scale = Vector3.Zero;
                }

                var fn = (TimingFunction)easingFunction;
                var animation = new ZoomAnimation(transform, scale, finalScale, duration, fn);
                entity.AddComponent(animation);
            }
            else
            {
                scale = new Vector3(dstScaleX, dstScaleY, 1);
            }
        }

        public override void Rotate(
            string entityName, TimeSpan duration,
            NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                RotateCore(entity, duration, dstRotationX, dstRotationY, dstRotationZ, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, duration);
            }
        }

        private static void RotateCore(
            Entity entity, TimeSpan duration,
            NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
            NsEasingFunction easingFunction)
        {
            var transform = entity.Transform;
            ref var rotation = ref transform.Rotation;

            var finalValue = rotation;
            dstRotationY *= -1;
            dstRotationX.AssignTo(ref finalValue.X);
            dstRotationY.AssignTo(ref finalValue.Y);
            dstRotationZ.AssignTo(ref finalValue.Z);

            if (duration > TimeSpan.Zero)
            {
                var fn = (TimingFunction)easingFunction;
                var animation = new Vector3Animation(
                    transform,
                    (t, v) => (t as Transform).Rotation = v,
                    rotation, finalValue, duration, fn);

                entity.AddComponent(animation);
            }
            else
            {
                rotation = finalValue;
            }
        }

        public override void MoveCube(
            string entityName, TimeSpan duration,
            NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            foreach (var entity in _entities.Query(entityName))
            {
                MoveCubeCore(entity, duration, dstTranslationX, dstTranslationY, dstTranslationZ, easingFunction);
            }

            if (delay > TimeSpan.Zero)
            {
                Interpreter.SuspendThread(CurrentThread, duration);
            }
        }

        private static void MoveCubeCore(
            Entity entity, TimeSpan duration,
            NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ,
            NsEasingFunction easingFunction)
        {
            var transform = entity.Transform;
            var initialValue = transform.Position;
            var finalValue = initialValue;
            dstTranslationX *= 0.001f;
            dstTranslationY *= 0.001f;
            dstTranslationZ *= 0.001f;
            dstTranslationX.AssignTo(ref finalValue.X);
            dstTranslationY.AssignTo(ref finalValue.Y);
            dstTranslationZ.AssignTo(ref finalValue.Z);

            var fn = (TimingFunction)easingFunction;
            var animation = new MoveAnimation(transform, initialValue, finalValue, duration, fn);
            entity.AddComponent(animation);
        }

        public override void DrawTransition(
            string sourceEntityName, TimeSpan duration,
            NsRational initialOpacity, NsRational finalOpacity,
            NsRational feather, NsEasingFunction easingFunction,
            string maskFileName, TimeSpan delay)
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
