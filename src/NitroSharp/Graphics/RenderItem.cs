using System;
using System.Numerics;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal readonly struct RenderItemKey : IComparable<RenderItemKey>
    {
        public readonly int Priority;
        public readonly int Id;

        public RenderItemKey(int priority, int id)
            => (Priority, Id) = (priority, id);

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

        public RenderItemKey Key => _key;
        public ref RgbaFloat Color => ref _color;
        public ref Transform Transform => ref _transform;

        protected virtual AnimPropagationMode AnimPropagationMode
            => AnimPropagationMode.ScaleRotateFade;

        public override bool IsIdle => true;

        public bool IsHidden { get; private set; }

        public virtual bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Rotate => _rotateAnim is object,
            AnimationKind.Fade => _fadeAnim is object,
            _ => false
        };

        public void Hide() => IsHidden = true;
        public void Reveal() => IsHidden = false;

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
            LayoutPass(ctx.RenderContext);
        }

        protected virtual void Update(GameContext ctx)
        {
        }

        protected virtual void LayoutPass(RenderContext ctx)
        {
        }

        public virtual void Render(RenderContext ctx, bool assetsReady)
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
    }

    internal abstract class RenderItem2D : RenderItem
    {
        private RenderTarget? _offscreenTarget;
        private QuadGeometry _quad;
        private Matrix4x4 _worldMatrix;

        private MoveAnimation? _moveAnim;
        private ScaleAnimation? _scaleAnim;
        private BezierMoveAnimation? _bezierMoveAnim;

        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(path, priority)
        {
            if (Parent is Choice choice)
            {
                switch (Id.MouseState)
                {
                    case MouseState.Normal:
                        choice.DefaultVisual = this;
                        break;
                    case MouseState.Over:
                        choice.AddMouseOver(this);
                        break;
                    case MouseState.Down:
                        choice.AddMouseDown(this);
                        break;
                }
            }
        }

        protected RectangleF BoundingRect { get; private set; }
        protected ref QuadGeometry Quad => ref _quad;
        protected ref Matrix4x4 WorldMatrix => ref _worldMatrix;

        public BlendMode BlendMode { get; set; }
        public FilterMode FilterMode { get; set; }

        public override bool IsAnimationActive(AnimationKind kind) => kind switch
        {
            AnimationKind.Move => _moveAnim is object,
            AnimationKind.Zoom => _scaleAnim is object,
            AnimationKind.BezierMove => _bezierMoveAnim is object,
            _ => base.IsAnimationActive(kind)
        };

        public override bool IsIdle
            => _moveAnim is null && _scaleAnim is null
                && _rotateAnim is null && _fadeAnim is null;

        protected virtual bool PreciseHitTest => false;

        public abstract Size GetUnconstrainedBounds(RenderContext ctx);

        protected virtual (Vector2, Vector2) GetTexCoords(RenderContext ctx)
            => (Vector2.Zero, Vector2.One);

        protected override void AdvanceAnimations(RenderContext ctx, float dt, bool assetsReady)
        {
            base.AdvanceAnimations(ctx, dt, assetsReady);
            AdvanceAnimation(ref _moveAnim, dt);
            AdvanceAnimation(ref _scaleAnim, dt);
            AdvanceAnimation(ref _rotateAnim, dt);
            AdvanceAnimation(ref _bezierMoveAnim, dt);
        }

        protected override void LayoutPass(RenderContext ctx)
        {
            base.LayoutPass(ctx);
            if (!(Parent is RenderItem))
            {
                Layout(ctx, constraintRect: null);
            }
        }

        private void Layout(RenderContext ctx, RectangleF? constraintRect)
        {
            Size unconstrainedBounds = GetUnconstrainedBounds(ctx);
            WorldMatrix = Transform.GetMatrix(unconstrainedBounds);
            (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetTexCoords(ctx);
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

            foreach (RenderItem2D child in GetChildren<RenderItem2D>())
            {
                child.Layout(ctx, constraintRect);
            }
        }

        public bool HitTest(RenderContext ctx, InputContext input)
        {
            return BoundingRect.Contains(input.MousePosition);
        }

        public override void Render(RenderContext ctx, bool assetsReady)
        {
            if (Color.A > 0.0f)
            {
                Render(ctx, ctx.MainBatch);
            }

            return;

            SizeF actualSize = BoundingRect.Size;
            if (actualSize.Width <= 0.0f || actualSize.Height <= 0.0f)
            {
                return;
            }

            //if (RenderOffscreen(ctx) is Texture tex)
            //{
            //    ctx.MainBatch.PushQuad(
            //        Quad,
            //        tex,
            //        alphaMask: ctx.WhiteTexture,
            //        BlendMode,
            //        FilterMode
            //    );
            //}
        }

        protected Texture RenderOffscreen(RenderContext ctx)
        {
            var actualSize = BoundingRect.Size.ToSize();
            Vector3 translation = _worldMatrix.Translation;
            if (_offscreenTarget is null || !_offscreenTarget.Size.Equals(actualSize))
            {
                _offscreenTarget?.Dispose();
                _offscreenTarget = new RenderTarget(ctx.GraphicsDevice, actualSize);
                using (DrawBatch batch = ctx.BeginBatch(_offscreenTarget, RgbaFloat.Clear))
                {
                    _worldMatrix.Translation = Vector3.Zero;
                    (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetTexCoords(ctx);
                    (Quad, _) = QuadGeometry.Create(
                        BoundingRect.Size,
                        _worldMatrix,
                        uvTopLeft,
                        uvBottomRight,
                        color: Vector4.One
                    );

                    Render(ctx, batch);
                }
            }

            _worldMatrix = Matrix4x4.CreateTranslation(translation);
            (Quad, _) = QuadGeometry.Create(
                BoundingRect.Size,
                _worldMatrix,
                uvTopLeft: Vector2.Zero,
                uvBottomRight: Vector2.One,
                Color.ToVector4()
            );

            return _offscreenTarget.ColorTarget;
        }

        protected virtual void Render(RenderContext ctx, DrawBatch batch)
        {
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
                NsCoordinate { Kind: NsCoordinateKind.Value, Value: var val }
                    => val.isRelative ? pos.X + val.pos : origin.X + val.pos,
                NsCoordinate { Kind: NsCoordinateKind.Inherit } => origin.X,
                NsCoordinate { Kind: NsCoordinateKind.Alignment, Alignment: var align } => align switch
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
                NsCoordinate { Kind: NsCoordinateKind.Value, Value: var val }
                    => val.isRelative ? pos.Y + val.pos : origin.Y + val.pos,
                NsCoordinate { Kind: NsCoordinateKind.Inherit } => origin.Y,
                NsCoordinate { Kind: NsCoordinateKind.Alignment, Alignment: var align } => align switch
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
}
