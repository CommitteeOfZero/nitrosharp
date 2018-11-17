using NitroSharp.NsScript;
using System;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Logic.Components;
using NitroSharp.Primitives;
using Veldrid;
using NitroSharp.Graphics;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        public override void Fade(
            string entityName, TimeSpan duration, NsRational opacity,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            bool wait = delay == duration;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                if (entity.IsVisual)
                {
                    FadeCore(entity, duration, opacity, easingFunction, wait);
                }
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    Interpreter.SuspendThread(CurrentThread);
                }
                else
                {
                    Interpreter.SuspendThread(CurrentThread, delay);
                }
            }
        }

        private FadeAnimation FadeCore(
            Entity entity, TimeSpan duration, NsRational dstOpacity,
            NsEasingFunction easingFunction, bool wait)
        {
            if (!entity.IsVisual) { return null; }
            RenderItemTable table = _world.GetTable<RenderItemTable>(entity);
            float adjustedOpacity = dstOpacity.Rebase(1.0f);
            RgbaFloat color = table.Colors.GetValue(entity);
            if (duration > TimeSpan.Zero)
            {
                var animation = new FadeAnimation(entity, duration, (TimingFunction)easingFunction);
                animation.InitialOpacity = color.A;
                animation.FinalOpacity = adjustedOpacity;
                animation.IsBlocking = CurrentThread == MainThread;
                if (wait)
                {
                    animation.WaitingThread = CurrentThread;
                }
                _world.ActivateAnimation(animation);
                return animation;
            }
            else
            {
                color.SetAlpha(adjustedOpacity);
                table.Colors.Set(entity, ref color);
                return null;
            }
        }

        public override void Move(
            string entityName, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            bool wait = delay == duration;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                if (entity.IsVisual)
                {
                    MoveCore(entity, duration, dstX, dstY, easingFunction, wait);
                }
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    Interpreter.SuspendThread(CurrentThread);
                }
                else
                {
                    Interpreter.SuspendThread(CurrentThread, delay);
                }
            }
        }

        private void MoveCore(
            Entity entity, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, bool wait)
        {
            RenderItemTable table = _world.GetTable<RenderItemTable>(entity);
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
                animation.IsBlocking = CurrentThread == MainThread;
                if (wait)
                {
                    animation.WaitingThread = CurrentThread;
                }
                _world.ActivateAnimation(animation);
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
            bool wait = delay == duration;
            foreach ((Entity entity, string name) in _world.Query(entityName))
            {
                if (entity.IsVisual)
                {
                    ZoomCore(entity, duration, dstScaleX, dstScaleY, easingFunction, wait);
                }
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    Interpreter.SuspendThread(CurrentThread);
                }
                else
                {
                    Interpreter.SuspendThread(CurrentThread, delay);
                }
            }
        }

        private void ZoomCore(
            Entity entity, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction, bool suspendThread)
        {
            RenderItemTable table = _world.GetTable<RenderItemTable>(entity);
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
                animation.IsBlocking = CurrentThread == MainThread;
                if (suspendThread)
                {
                    animation.WaitingThread = CurrentThread;
                }
                _world.ActivateAnimation(animation);
            }
            else
            {
                scale = new Vector3(dstScaleX, dstScaleY, 1);
            }
        }

        //private void WaitForAnimation(ThreadContext thread, PropertyAnimation animation)
        //{
        //    Interpreter.SuspendThread(thread);
        //    animation.Completed += () =>
        //    {
        //        Interpreter.ResumeThread(thread);
        //    };
        //}

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
