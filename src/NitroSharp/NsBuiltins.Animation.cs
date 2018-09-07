using NitroSharp.NsScript;
using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Logic.Components;
using NitroSharp.Primitives;
using Veldrid;
using NitroSharp.Graphics;
using NitroSharp.NsScript.Execution;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        public override void Fade(
            string entityName, TimeSpan duration, NsRational opacity,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            FadeAnimation lastAnimation = null;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                lastAnimation = FadeCore(entity, duration, opacity, easingFunction);
            }

            if (delay > TimeSpan.Zero && lastAnimation != null)
            {
                WaitForAnimation(CurrentThread, lastAnimation);
            }
        }

        private FadeAnimation FadeCore(
            Entity entity, TimeSpan duration, NsRational dstOpacity, NsEasingFunction easingFunction)
        {
            float adjustedOpacity = dstOpacity.Rebase(1.0f);
            Visuals table = _world.GetTable<Visuals>(entity);
            ref RgbaFloat color = ref table.Colors.Mutate(entity);
            if (duration > TimeSpan.Zero)
            {
                var animation = new FadeAnimation(entity, duration, (TimingFunction)easingFunction);
                animation.InitialOpacity = color.A;
                animation.FinalOpacity = adjustedOpacity;
                _world.ActivateBehavior(animation);
                return animation;
            }
            else
            {
                color.SetAlpha(adjustedOpacity);
                return null;
            }
        }

        public override void Move(
            string entityName, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            MoveAnimation lastAnimation = null;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                lastAnimation = MoveCore(entity, duration, dstX, dstY, easingFunction);
            }

            if (delay > TimeSpan.Zero && lastAnimation != null)
            {
                WaitForAnimation(CurrentThread, lastAnimation);
            }
        }

        private MoveAnimation MoveCore(
            Entity entity, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction)
        {
            Visuals table = _world.GetTable<Visuals>(entity);
            ref TransformComponents transform = ref table.TransformComponents.Mutate(entity);
            ref Vector3 position = ref transform.Position;

            float targetX = dstX.Origin == NsCoordinateOrigin.CurrentValue
                ? position.X + dstX.Value
                : dstX.Value;

            float targetY = dstY.Origin == NsCoordinateOrigin.CurrentValue
                ? position.Y + dstY.Value
                : dstY.Value;

            var destination = new Vector3(targetX, targetY, 0);
            if (duration > TimeSpan.Zero)
            {
                var animation = new MoveAnimation(entity, duration, (TimingFunction)easingFunction);
                animation.StartPosition = position;
                animation.Destination = destination;
                _world.ActivateBehavior(animation);
                return animation;
            }
            else
            {
                position = destination;
                return null;
            }
        }

        public override void Zoom(
            string entityName, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            ZoomAnimation lastAnimation = null;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                lastAnimation = ZoomCore(entity, duration, dstScaleX, dstScaleY, easingFunction);
            }

            if (delay > TimeSpan.Zero && lastAnimation != null)
            {
                WaitForAnimation(CurrentThread, lastAnimation);
            }
        }

        private ZoomAnimation ZoomCore(
            Entity entity, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction)
        {
            Visuals table = _world.GetTable<Visuals>(entity);
            ref TransformComponents transform = ref table.TransformComponents.Mutate(entity);
            ref Vector3 scale = ref transform.Scale;

            dstScaleX = dstScaleX.Rebase(1.0f);
            dstScaleY = dstScaleY.Rebase(1.0f);

            if (duration > TimeSpan.Zero)
            {
                Vector3 finalScale = new Vector3(dstScaleX, dstScaleY, 1);
                if (scale == finalScale)
                {
                    scale = Vector3.Zero;
                }

                var animation = new ZoomAnimation(entity, duration, (TimingFunction)easingFunction);
                animation.InitialScale = scale;
                animation.FinalScale = finalScale;
                _world.ActivateBehavior(animation);
                return animation;
            }
            else
            {
                scale = new Vector3(dstScaleX, dstScaleY, 1);
                return null;
            }
        }

        private void WaitForAnimation(ThreadContext thread, AnimationBase animation)
        {
            Interpreter.SuspendThread(thread);
            animation.Completed += () =>
            {
                Interpreter.ResumeThread(thread);
            };
        }

        //public override void Rotate(
        //    string entityName, TimeSpan duration,
        //    NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
        //    NsEasingFunction easingFunction, TimeSpan delay)
        //{
        //    foreach (var entity in _world.Query(entityName))
        //    {
        //        RotateCore(entity, duration, dstRotationX, dstRotationY, dstRotationZ, easingFunction);
        //    }

        //    if (delay > TimeSpan.Zero)
        //    {
        //        Interpreter.SuspendThread(CurrentThread, duration);
        //    }
        //}

        //private static void RotateCore(
        //    OldEntity entity, TimeSpan duration,
        //    NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
        //    NsEasingFunction easingFunction)
        //{
        //    var transform = entity.Transform;
        //    ref var rotation = ref transform.Rotation;

        //    var finalValue = rotation;
        //    dstRotationY *= -1;
        //    dstRotationX.AssignTo(ref finalValue.X);
        //    dstRotationY.AssignTo(ref finalValue.Y);
        //    dstRotationZ.AssignTo(ref finalValue.Z);

        //    if (duration > TimeSpan.Zero)
        //    {
        //        var fn = (TimingFunction)easingFunction;
        //        var animation = new RotateAnimation(transform, rotation, finalValue, duration, fn);
        //        entity.AddComponent(animation);
        //    }
        //    else
        //    {
        //        rotation = finalValue;
        //    }
        //}

        //public override void MoveCube(
        //    string entityName, TimeSpan duration,
        //    NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ,
        //    NsEasingFunction easingFunction, TimeSpan delay)
        //{
        //    foreach (var entity in _world.Query(entityName))
        //    {
        //        MoveCubeCore(entity, duration, dstTranslationX, dstTranslationY, dstTranslationZ, easingFunction);
        //    }

        //    if (delay > TimeSpan.Zero)
        //    {
        //        Interpreter.SuspendThread(CurrentThread, duration);
        //    }
        //}

        //private static void MoveCubeCore(
        //    OldEntity entity, TimeSpan duration,
        //    NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ,
        //    NsEasingFunction easingFunction)
        //{
        //    var transform = entity.Transform;
        //    var initialValue = transform.Position;
        //    var finalValue = initialValue;
        //    dstTranslationX *= 0.001f;
        //    dstTranslationY *= 0.001f;
        //    dstTranslationZ *= 0.001f;
        //    dstTranslationX.AssignTo(ref finalValue.X);
        //    dstTranslationY.AssignTo(ref finalValue.Y);
        //    dstTranslationZ.AssignTo(ref finalValue.Z);

        //    var fn = (TimingFunction)easingFunction;
        //    var animation = new MoveAnimation(transform, initialValue, finalValue, duration, fn);
        //    entity.AddComponent(animation);
        //}

        //public override void DrawTransition(
        //    string sourceEntityName, TimeSpan duration,
        //    NsRational initialOpacity, NsRational finalOpacity,
        //    NsRational feather, NsEasingFunction easingFunction,
        //    string maskFileName, TimeSpan delay)
        //{
        //    initialOpacity = initialOpacity.Rebase(1.0f);
        //    finalOpacity = finalOpacity.Rebase(1.0f);

        //    initialOpacity = initialOpacity.Rebase(1.0f);
        //    finalOpacity = finalOpacity.Rebase(1.0f);

        //    if (delay > TimeSpan.Zero)
        //    {
        //        Interpreter.SuspendThread(CurrentThread, delay);
        //    }
        //}
    }
}
