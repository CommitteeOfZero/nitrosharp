using System;
using System.Numerics;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp.Graphics
{
    [Persistable]
    internal readonly partial record struct RenderItemKey(int Priority, int Id) : IComparable<RenderItemKey>
    {
        public int CompareTo(RenderItemKey other)
        {
            if (Priority > other.Priority) { return 1; }
            if (Priority < other.Priority) { return -1; }
            if (Id > other.Id) { return 1; }
            return -1;
        }
    }

    internal enum AnimPropagationMode
    {
        None,
        ScaleRotateFade,
        All
    }

    internal abstract class RenderItem : Entity, IComparable<RenderItem>
    {
        private static int s_lastId;

        private readonly RenderItemKey _key;
        private Transform _transform;
        private RgbaFloat _color;

        protected RotateAnimation? _rotateAnim;
        protected OpacityAnimation? _fadeAnim;

        protected RenderItem(in ResolvedEntityPath path, int priority)
            : base(path)
        {
            _key = new RenderItemKey(priority, s_lastId++);
            _color = RgbaFloat.White;
            _transform = Transform.Default;
        }

        protected RenderItem(in ResolvedEntityPath path, in RenderItemSaveData saveData)
            : base(path, saveData.EntityData)
        {
            _key = saveData.Key;
            _transform = saveData.Transform;
            _color = new RgbaFloat(saveData.Color);
            if (saveData.RotateAnimation is { } rotateAnim)
            {
                _rotateAnim = new RotateAnimation(this, rotateAnim);
            }
            if (saveData.FadeAnimation is { } fadeAnim)
            {
                _fadeAnim = new OpacityAnimation(this, fadeAnim);
            }

            s_lastId = Math.Max(s_lastId, _key.Id);
        }

        public RenderItemKey Key => _key;
        public ref RgbaFloat Color => ref _color;
        public ref Transform Transform => ref _transform;

        protected virtual AnimPropagationMode AnimPropagationMode
            => AnimPropagationMode.ScaleRotateFade;

        public override bool IsIdle => true;

        public bool IsHidden { get; private set; }

        public virtual bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Rotate => _rotateAnim is not null,
            AnimationKind.Fade => _fadeAnim is not null,
            _ => false
        };

        public void Hide()
        {
            IsHidden = true;
        }

        public void Reveal()
        {
            IsHidden = false;
        }

        protected virtual void AdvanceAnimations(RenderContext ctx, float dt, bool assetsReady)
        {
            AdvanceAnimation(ref _fadeAnim, dt);
            AdvanceAnimation(ref _rotateAnim, dt);
        }

        protected static void AdvanceAnimation<T>(ref T? anim, float dt)
            where T : Animation
        {
            if (anim?.Update(dt) is false)
            {
                if (anim is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                anim = null;
            }
        }

        public void Update(GameContext ctx, float dt, bool assetsReady)
        {
            AdvanceAnimations(ctx.RenderContext, dt, assetsReady);
            Update(ctx);
            PerformLayout(ctx);
        }

        protected virtual void Update(GameContext ctx)
        {
        }

        public virtual void PerformLayout(GameContext ctx)
        {
        }

        public void Render(RenderContext ctx)
        {
            Render(ctx, ctx.MainBatch);
        }

        public virtual void Render(RenderContext ctx, DrawBatch drawBatch)
        {
        }

        protected virtual void RenderCore(RenderContext ctx, DrawBatch drawBatch)
        {
        }

        public void Fade(
            float dstOpacity,
            TimeSpan duration,
            NsEaseFunction easeFunction = NsEaseFunction.Linear)
        {
            if (duration > TimeSpan.Zero)
            {
                _fadeAnim = new OpacityAnimation(this, _color.A, dstOpacity, duration, easeFunction);
            }
            else
            {
                _fadeAnim = null;
                _color.SetAlpha(dstOpacity);
            }

            if (AnimPropagationMode != AnimPropagationMode.None)
            {
                foreach (RenderItem child in GetChildren<RenderItem>())
                {
                    child.Fade(dstOpacity, duration, easeFunction);
                }
            }
        }

        public void Rotate(
            NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
            TimeSpan duration,
            NsEaseFunction easeFunction)
        {
            Vector3 dstRot = Transform.Rotation;
            // ¯\_(ツ)_/¯
            dstRotationY *= -1;
            dstRotationX.AssignTo(ref dstRot.X);
            dstRotationY.AssignTo(ref dstRot.Y);
            dstRotationZ.AssignTo(ref dstRot.Z);

            if (duration > TimeSpan.Zero)
            {
                Vector3 startRot = Transform.Rotation;
                _rotateAnim = new RotateAnimation(this, startRot, dstRot, duration, easeFunction);
            }
            else
            {
                _rotateAnim = null;
                Transform.Rotation = dstRot;
            }

            if (AnimPropagationMode != AnimPropagationMode.None)
            {
                foreach (RenderItem child in GetChildren<RenderItem>())
                {
                    child.Rotate(dstRotationX, dstRotationY, dstRotationZ, duration, easeFunction);
                }
            }
        }

        public int CompareTo(RenderItem? other)
            => _key.CompareTo(other!._key);

        protected new RenderItemSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            EntityData = base.ToSaveData(ctx),
            Key = Key,
            Color = Color.ToVector4(),
            Transform = Transform,
            FadeAnimation = _fadeAnim?.ToSaveData(),
            MoveAnimation = null,
            ZoomAnimation = null,
            RotateAnimation = _rotateAnim?.ToSaveData(),
            BezierMoveAnimation = null
        };
    }

    internal abstract class RenderItem2D : RenderItem
    {
        private RenderTarget? _offscreenTarget;
        private Fence? _fence;
        private QuadGeometry _quad;
        private Matrix4x4 _worldMatrix;

        private MoveAnimation? _moveAnim;
        private ScaleAnimation? _scaleAnim;
        private BezierMoveAnimation? _bezierMoveAnim;

        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(path, priority)
        {
        }

        protected RenderItem2D(in ResolvedEntityPath path, in RenderItemSaveData saveData)
            : base(path, saveData)
        {
            BlendMode = saveData.BlendMode;
            FilterMode = saveData.FilterMode;
            if (saveData.MoveAnimation is { } moveAnim)
            {
                _moveAnim  = new MoveAnimation(this, moveAnim);
            }
            if (saveData.ZoomAnimation is { } zoomAnim)
            {
                _scaleAnim  = new ScaleAnimation(this, zoomAnim);
            }
            if (saveData.BezierMoveAnimation is { } bezierMoveAnim)
            {
                _bezierMoveAnim  = new BezierMoveAnimation(this, bezierMoveAnim);
            }
        }

        public RectangleF BoundingRect { get; private set; }

        protected ref QuadGeometry Quad => ref _quad;
        protected ref Matrix4x4 WorldMatrix => ref _worldMatrix;

        public BlendMode BlendMode { get; set; }
        public FilterMode FilterMode { get; set; }

        public override bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Move => _moveAnim is not null,
            AnimationKind.Zoom => _scaleAnim is not null,
            AnimationKind.BezierMove => _bezierMoveAnim is not null,
            _ => base.IsAnimationActive(kind)
        };

        public override bool IsIdle
            => _moveAnim is null && _scaleAnim is null && _rotateAnim is null && _fadeAnim is null;

        protected virtual bool PreciseHitTest => false;

        public abstract Size GetUnconstrainedBounds(RenderContext ctx);

        protected virtual (Vector2, Vector2) GetTexCoords(RenderContext ctx)
            => (Vector2.Zero, Vector2.One);

        protected AlphaMask? TryGetAlphaMaskAscendant()
        {
            Entity? current = Parent;
            while (current is not (AlphaMask or null))
            {
                current = current.Parent;
            }

            return current as AlphaMask;
        }

        protected override void AdvanceAnimations(RenderContext ctx, float dt, bool assetsReady)
        {
            base.AdvanceAnimations(ctx, dt, assetsReady);
            AdvanceAnimation(ref _moveAnim, dt);
            AdvanceAnimation(ref _scaleAnim, dt);
            AdvanceAnimation(ref _rotateAnim, dt);
            AdvanceAnimation(ref _bezierMoveAnim, dt);
        }

        public override void PerformLayout(GameContext ctx)
        {
            base.PerformLayout(ctx);
            if (Parent is ConstraintBox || TryGetOwningChoice() is { Parent: ConstraintBox })
            {
                return;
            }

            PerformLayout(ctx, constraintRect: null);
        }

        private void PerformLayout(GameContext ctx, RectangleF? constraintRect)
        {
            Size unconstrainedBounds = GetUnconstrainedBounds(ctx.RenderContext);
            WorldMatrix = Transform.GetMatrix(unconstrainedBounds);
            (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetTexCoords(ctx.RenderContext);
            (Quad, BoundingRect) = QuadGeometry.Create(
                unconstrainedBounds.ToSizeF(),
                WorldMatrix,
                uvTopLeft,
                uvBottomRight,
                Color.ToVector4(),
                constraintRect
            );

            if (this is ConstraintBox)
            {
                constraintRect = BoundingRect;
            }

            World world = ctx.ActiveProcess.World;
            foreach (Entity child in GetChildren())
            {
                if (child is RenderItem2D renderItem)
                {
                    renderItem.PerformLayout(ctx, constraintRect);
                }
                else if (child.UiElement is Choice choice)
                {
                    if (choice.TryGetMouseUsualVisual(world) is { } mouseUsual)
                    {
                        mouseUsual.PerformLayout(ctx, constraintRect);
                    }

                    foreach (RenderItem2D mouseOver in choice.QueryMouseOverVisuals(world))
                    {
                        mouseOver.PerformLayout(ctx, constraintRect);
                    }

                    foreach (RenderItem2D mouseClick in choice.QueryMouseClickVisuals(world))
                    {
                        mouseClick.PerformLayout(ctx, constraintRect);
                    }
                }
            }
        }

        public bool HitTest(RenderContext ctx, InputContext input)
        {
            return PreciseHitTest && _offscreenTarget is { } offscreenTarget
                ? PixelPerfectHitTest(ctx, offscreenTarget, input)
                : BoundingRect.Contains(input.MousePosition);
        }

        private bool PixelPerfectHitTest(RenderContext ctx, RenderTarget offscreenTarget, InputContext input)
        {
            _fence ??= ctx.ResourceFactory.CreateFence(signaled: false);
            CommandList cl = ctx.CommandListPool.Rent();
            cl.Begin();
            Texture tex = offscreenTarget.ReadBack(cl, ctx.ResourceFactory);
            cl.End();
            ctx.GraphicsDevice.SubmitCommands(cl, _fence);
            ctx.GraphicsDevice.WaitForFence(_fence);
            _fence.Reset();
            ctx.CommandListPool.Return(cl);

            MappedResourceView<RgbaByte> map = ctx.GraphicsDevice.Map<RgbaByte>(tex, MapMode.Read);
            Vector2 pos = input.MousePosition - _worldMatrix.Translation.XY();
            if (pos.X < 0.0f || pos.X >= tex.Width || pos.Y < 0.0f || pos.Y >= tex.Height)
            {
                ctx.GraphicsDevice.Unmap(tex);
                return false;
            }
            RgbaByte pixel = map[(int)pos.X, (int)pos.Y];
            ctx.GraphicsDevice.Unmap(tex);
            return !pixel.Equals(default);
        }

        public override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            SizeF actualSize = BoundingRect.Size;
            if (actualSize.Width <= 0.0f || actualSize.Height <= 0.0f)
            {
                return;
            }

            if (PreciseHitTest && ReferenceEquals(drawBatch, ctx.MainBatch))
            {
                RenderOffscreen(ctx);
            }

            if (!IsHidden)
            {
                RenderCore(ctx, drawBatch);
            }
        }

        protected Texture RenderOffscreen(RenderContext ctx)
        {
            var actualSize = BoundingRect.Size.ToSize();
            if (_offscreenTarget is null || !_offscreenTarget.Size.Equals(actualSize))
            {
                _offscreenTarget?.Dispose();
                _offscreenTarget = new RenderTarget(ctx.GraphicsDevice, actualSize);
            }

            using (DrawBatch batch = ctx.BeginOffscreenBatch(_offscreenTarget, RgbaFloat.Clear))
            {
                Matrix4x4 world = _worldMatrix;
                world.Translation = Vector3.Zero;
                (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetTexCoords(ctx);
                QuadGeometry originalQuad = Quad;
                (Quad, _) = QuadGeometry.Create(
                    BoundingRect.Size,
                    world,
                    uvTopLeft,
                    uvBottomRight,
                    color: Vector4.One
                );

                RenderCore(ctx, batch);
                Quad = originalQuad;
            }

            //(Quad, _) = QuadGeometry.Create(
            //    BoundingRect.Size,
            //    Matrix4x4.CreateTranslation(_worldMatrix.Translation),
            //    uvTopLeft: Vector2.Zero,
            //    uvBottomRight: Vector2.One,
            //    Color.ToVector4()
            //);

            return _offscreenTarget.ColorTarget;
        }

        public Vector3 Point(RenderContext ctx, NsCoordinate x, NsCoordinate y)
        {
            Vector3 pos = Transform.Position;
            Size screenBounds = ctx.DesignResolution;
            Size parentBounds = Parent is RenderItem2D parentVisual
                ? parentVisual.GetUnconstrainedBounds(ctx)
                : screenBounds;
            Vector3 origin = Parent switch
            {
                ConstraintBox { IsContainer: false } => Vector3.Zero,
                RenderItem2D parent => parent.Transform.Position,
                _ => Vector3.Zero
            };
            pos.X = x switch
            {
                { Kind: NsCoordinateKind.Value, Value: var val }
                    => val.isRelative ? pos.X + val.pos : origin.X + val.pos,
                { Kind: NsCoordinateKind.Inherit } => origin.X,
                { Kind: NsCoordinateKind.Alignment, Alignment: var align } => align switch
                    {
                        NsAlignment.Left => origin.X,
                        NsAlignment.Center => screenBounds.Width / 2.0f,
                        NsAlignment.Right => origin.X + parentBounds.Width,
                        _ => ThrowHelper.UnexpectedValue<float>()
                    },
                _ => 0.0f
            };
            pos.Y = y switch
            {
                { Kind: NsCoordinateKind.Value, Value: var val }
                    => val.isRelative ? pos.Y + val.pos : origin.Y + val.pos,
                { Kind: NsCoordinateKind.Inherit } => origin.Y,
                { Kind: NsCoordinateKind.Alignment, Alignment: var align } => align switch
                    {
                        NsAlignment.Top => origin.Y,
                        NsAlignment.Center => screenBounds.Height / 2.0f,
                        NsAlignment.Bottom => origin.Y + parentBounds.Height,
                        _ => ThrowHelper.UnexpectedValue<float>()
                    },
                _ => 0.0f
            };
            var anchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);
            var bounds = GetUnconstrainedBounds(ctx).ToVector2();
            // N2: actual bounds of a dialogue box are ignored when computing its final position.
            if (this is DialogueBox)
            {
                bounds = ctx.DesignResolution.ToVector2();
            }

            pos -= new Vector3(anchorPoint * bounds, 0);
            return pos;
        }

        public void Move(
            RenderContext ctx,
            in NsCoordinate x, in NsCoordinate y,
            TimeSpan duration,
            NsEaseFunction easeFunction)
        {
            if (AnimPropagationMode != AnimPropagationMode.None)
            {
                foreach (RenderItem2D child in GetChildren<RenderItem2D>())
                {
                    child.Move(ctx, x, y, duration, easeFunction);
                }
            }

            MoveCore(Point(ctx, x, y), duration, easeFunction);
        }

        private void MoveCore(in Vector3 destination, TimeSpan duration, NsEaseFunction easeFunction)
        {
            if (duration > TimeSpan.Zero)
            {
                Vector3 startPosition = Transform.Position;
                _moveAnim = new MoveAnimation(this, startPosition, destination, duration, easeFunction);
            }
            else
            {
                _moveAnim = null;
                Transform.Position = destination;
            }
        }

        public void Scale(in Vector3 dstScale, TimeSpan duration, NsEaseFunction easeFunction)
        {
            if (duration > TimeSpan.Zero)
            {
                Vector3 startScale = Transform.Scale;
                _scaleAnim = new ScaleAnimation(this, startScale, dstScale, duration, easeFunction);
            }
            else
            {
                _scaleAnim = null;
                Transform.Scale = dstScale;
            }

            if (AnimPropagationMode != AnimPropagationMode.None)
            {
                foreach (RenderItem2D child in GetChildren<RenderItem2D>())
                {
                    child.Scale(dstScale, duration, easeFunction);
                }
            }
        }

        public void BezierMove(
            in ProcessedBezierCurve curve,
            TimeSpan duration,
            NsEaseFunction easeFunction)
        {
            _bezierMoveAnim = new BezierMoveAnimation(this, curve, duration, easeFunction);
        }

        public override void Dispose()
        {
            _offscreenTarget?.Dispose();
        }

        protected new RenderItemSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            EntityData = (this as Entity).ToSaveData(ctx),
            Key = Key,
            Color = Color.ToVector4(),
            Transform = Transform,
            BlendMode = BlendMode,
            FilterMode = FilterMode,
            FadeAnimation = _fadeAnim?.ToSaveData(),
            MoveAnimation = _moveAnim?.ToSaveData(),
            ZoomAnimation = _scaleAnim?.ToSaveData(),
            RotateAnimation = _rotateAnim?.ToSaveData(),
            BezierMoveAnimation = _bezierMoveAnim?.ToSaveData()
        };
    }

    internal static class RenderItemExt
    {
        public static T WithPosition<T>(
            this T renderItem,
            RenderContext ctx,
            NsCoordinate x,
            NsCoordinate y)
            where T : RenderItem2D
        {
            renderItem.Transform.Position = renderItem.Point(ctx, x, y);
            return renderItem;
        }
    }

    [Persistable]
    internal readonly partial struct RenderItemSaveData
    {
        public EntitySaveData EntityData { get; init; }
        public Vector4 Color { get; init; }
        public RenderItemKey Key { get; init; }
        public Transform Transform { get; init; }
        public BlendMode BlendMode { get; init; }
        public FilterMode FilterMode { get; init; }
        public FloatAnimationSaveData? FadeAnimation { get; init; }
        public Vector3AnimationSaveData? MoveAnimation { get; init; }
        public Vector3AnimationSaveData? ZoomAnimation { get; init; }
        public Vector3AnimationSaveData? RotateAnimation { get; init; }
        public BezierAnimationSaveData? BezierMoveAnimation { get; init; }
    }
}
