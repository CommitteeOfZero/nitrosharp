using NitroSharp.NsScript;
using System;
using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Experimental;
using NitroSharp.Graphics;
using NitroSharp.Animation;
using System.Collections.Immutable;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        public override void BezierMove(
            string entityQuery,
            TimeSpan duration,
            CompositeBezier curve,
            NsEasingFunction easingFunction,
            bool wait)
        {
            foreach ((Entity entity, _) in QueryEntities(entityQuery))
            {
                BezierMoveCore(entity, duration, curve, easingFunction, wait);
            }
            if (duration > TimeSpan.Zero && wait)
            {
                VM.SuspendThread(CurrentThread);
            }
        }

        private void BezierMoveCore(
            Entity entity,
            TimeSpan duration,
            CompositeBezier curve,
            NsEasingFunction easingFunction,
            bool wait)
        {
            if (duration <= TimeSpan.Zero) { return; }
            var segments = ImmutableArray.CreateBuilder<ProcessedBezierSegment>();
            foreach (CubicBezierSegment srcSeg in curve.Segments)
            {
                Vector2 processPoint(BezierControlPoint srcPoint)
                    => GetAbsolutePosition(entity, srcPoint.X, srcPoint.Y);

                segments.Add(new ProcessedBezierSegment(
                    processPoint(srcSeg.P0),
                    processPoint(srcSeg.P1),
                    processPoint(srcSeg.P2),
                    processPoint(srcSeg.P3)
                ));
            }
            var processedCurve = new ProcessedBezierCurve(segments.ToImmutable());
            var storage = _world.GetStorage<RenderItemStorage>(entity);
            if (storage == null) { return; }
            var animation = new BezierMoveAnimation(entity, duration, easingFunction)
            {
                Curve = processedCurve,
                IsBlocking = CurrentThread == MainThread
            };
            if (wait)
            {
                animation.WaitingThread = CurrentThread;
            }
            _world.ActivateAnimation(animation);
        }

        public override void Fade(
            string entityName, TimeSpan duration, NsRational opacity,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            bool wait = delay == duration;
            foreach ((Entity entity, _) in QueryEntities(entityName))
            {
                FadeCore(entity, duration, opacity, easingFunction, wait);
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    VM.SuspendThread(CurrentThread);
                }
                else
                {
                    VM.SuspendThread(CurrentThread, delay);
                }
            }
        }

        private void FadeCore(
            Entity entity, TimeSpan duration, NsRational dstOpacity,
            NsEasingFunction easingFunction, bool wait)
        {
            var storage = _world.GetStorage<RenderItemStorage>(entity);
            if (storage == null) { return; }
            float adjustedOpacity = dstOpacity.Rebase(1.0f);
            ref RgbaFloat color = ref storage.Materials[entity].Color;
            if (duration > TimeSpan.Zero)
            {
                var animation = new FadeAnimation(entity, duration, easingFunction)
                {
                    InitialOpacity = color.A,
                    FinalOpacity = adjustedOpacity,
                    IsBlocking = CurrentThread == MainThread
                };
                if (wait)
                {
                    animation.WaitingThread = CurrentThread;
                }
                _world.ActivateAnimation(animation);
            }
            else
            {
                color.SetAlpha(adjustedOpacity);
                if (adjustedOpacity > 0.0f)
                {
                    _world.ScheduleEnableEntity(entity);
                }
                else
                {
                    _world.ScheduleDisableEntity(entity);
                }
            }
        }

        public override void Move(
            string entityName, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, TimeSpan delay)
        {
            bool wait = delay == duration;
            foreach ((Entity entity, _) in QueryEntities(entityName))
            {
                MoveCore(entity, duration, dstX, dstY, easingFunction, wait);
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    VM.SuspendThread(CurrentThread);
                }
                else
                {
                    VM.SuspendThread(CurrentThread, delay);
                }
            }
        }

#nullable enable

        private void MoveCore(
            Entity entity, TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEasingFunction easingFunction, bool wait)
        {
            RenderItemStorage? storage = _world.GetStorage<RenderItemStorage>(entity);
            if (storage == null) { return; }

            ref Vector3 position = ref storage.TransformComponents[entity].Position;
            float targetX = dstX.Origin == NsCoordinateOrigin.CurrentValue
                ? position.X + dstX.Value
                : dstX.Value;

            float targetY = dstY.Origin == NsCoordinateOrigin.CurrentValue
                ? position.Y + dstY.Value
                : dstY.Value;

            var destination = new Vector3(targetX, targetY, 0);
            if (duration > TimeSpan.Zero)
            {
                var animation = new MoveAnimation(entity, duration, easingFunction)
                {
                    StartPosition = position,
                    Destination = destination,
                    IsBlocking = CurrentThread == MainThread
                };
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
            foreach ((Entity entity, _) in QueryEntities(entityName))
            {
                ZoomCore(entity, duration, dstScaleX, dstScaleY, easingFunction, wait);
            }

            if (delay > TimeSpan.Zero)
            {
                if (wait)
                {
                    VM.SuspendThread(CurrentThread);
                }
                else
                {
                    VM.SuspendThread(CurrentThread, delay);
                }
            }
        }

        private void ZoomCore(
            Entity entity, TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction, bool suspendThread)
        {
            ref Vector3 scale = ref _world.GetStorage<RenderItemStorage>(entity)
                .TransformComponents[entity].Scale;
            dstScaleX = dstScaleX.Rebase(1.0f);
            dstScaleY = dstScaleY.Rebase(1.0f);

            if (duration > TimeSpan.Zero)
            {
                var finalScale = new Vector3(dstScaleX, dstScaleY, 1);
                if (scale == finalScale)
                {
                    scale = Vector3.Zero;
                }

                var animation = new ZoomAnimation(entity, duration, easingFunction)
                {
                    InitialScale = scale,
                    FinalScale = finalScale,
                    IsBlocking = CurrentThread == MainThread
                };
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
        //    foreach (var entity in Query(entityName))
        //    {
        //        RotateCore(entity, duration, dstRotationX, dstRotationY, dstRotationZ, easingFunction);
        //    }

        //    if (delay > TimeSpan.Zero)
        //    {
        //        Interpreter.SuspendThread(CurrentThread, duration);
        //    }
        //}

        //private static void RotateCore(
        //    Entity entity, TimeSpan duration,
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
        //        var fn = (NsEasingFunction)easingFunction;
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
        //    Entity entity, TimeSpan duration,
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

        //    var fn = (NsEasingFunction)easingFunction;
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
