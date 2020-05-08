using System;
using System.Numerics;
using NitroSharp.Content;
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

    [Flags]
    internal enum AnimPropagateFlags
    {
        Empty,
        Move,
        Scale,
        Rotate
    }

    internal abstract class RenderItem : Entity, IComparable<RenderItem>
    {
        private static int s_lastId;

        private readonly RenderItemKey _key;
        private Transform _transform;
        private RgbaFloat _color;

        protected RotateAnimation? _rotateAnim;

        protected RenderItem(in ResolvedEntityPath path, int priority)
            : base(path)
        {
            _key = new RenderItemKey(priority, s_lastId++);
            _color = RgbaFloat.White;
            _transform = Transform.Default;
        }

        public ref RgbaFloat Color => ref _color;
        public ref Transform Transform => ref _transform;

        public virtual AnimPropagateFlags AnimPropagateFlags => AnimPropagateFlags.Empty;
        public override bool IsIdle => true;
        public abstract bool IsMoving { get; }

        protected virtual void AdvanceAnimations(float dt)
        {
            AdvanceAnimation(ref _rotateAnim, dt);
        }

        protected static void AdvanceAnimation<T>(ref T? anim, float dt)
            where T : Animation
        {
            if (anim?.Update(dt) is false)
            {
                anim = null;
            }
        }

        public void Update(World world, RenderContext ctx, float dt)
        {
            AdvanceAnimations(dt);
            LayoutPass(world, ctx);
        }

        protected virtual void LayoutPass(World world, RenderContext ctx)
        {
        }

        public virtual void Render(RenderContext ctx)
        {
        }

        public void Rotate(in Vector3 dstRot, TimeSpan duration, NsEaseFunction easeFunction)
        {
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

        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(path, priority)
        {
        }

        private RectangleF BoundingRect { get; set; }
        protected ref QuadGeometry Quad => ref _quad;
        protected ref Matrix4x4 WorldMatrix => ref _worldMatrix;

        public BlendMode BlendMode { get; set; }
        public FilterMode FilterMode { get; set; }
        public AssetRef<Texture>? AlphaMaskOpt { get; set; }

        public override AnimPropagateFlags AnimPropagateFlags
            => AnimPropagateFlags.Scale;

        public override bool IsMoving => _moveAnim is object;

        public override bool IsIdle
            => _moveAnim is null && _scaleAnim is null && _rotateAnim is null;

        protected virtual bool PreciseHitTest => false;

        public abstract Size GetUnconstrainedBounds(RenderContext ctx);

        protected virtual (Vector2 uvTopLeft, Vector2 uvBottomRight) GetUV(RenderContext ctx)
            => (Vector2.Zero, Vector2.One);

        protected Texture GetAlphaMask(RenderContext ctx)
        {
            return AlphaMaskOpt is AssetRef<Texture> maskRef
                ? ctx.Content.Get(maskRef)
                : ctx.WhiteTexture;
        }

        protected override void AdvanceAnimations(float dt)
        {
            base.AdvanceAnimations(dt);
            AdvanceAnimation(ref _moveAnim, dt);
            AdvanceAnimation(ref _scaleAnim, dt);
            AdvanceAnimation(ref _rotateAnim, dt);
        }

        protected override void LayoutPass(World world, RenderContext ctx)
        {
            base.LayoutPass(world, ctx);
            if (!HasParent)
            {
                Layout(world, ctx, constraintRect: null);
            }
        }

        private void Layout(World world, RenderContext ctx, RectangleF? constraintRect)
        {
            Size unconstrainedBounds = GetUnconstrainedBounds(ctx);
            WorldMatrix = Transform.GetMatrix(unconstrainedBounds);
            (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetUV(ctx);
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
            foreach (RenderItem2D child in world.Children<RenderItem2D>(this))
            {
                child.Layout(world, ctx, constraintRect);
            }
        }

        public bool HitTest(RenderContext ctx, Vector2 mousePos)
        {
            if (!PreciseHitTest)
            {
                return BoundingRect.Contains(mousePos);
            }

            return false;
        }

        public override void Render(RenderContext ctx)
        {
            Render(ctx, ctx.MainBatch);
            return;

            SizeF actualSize = BoundingRect.Size;
            if (actualSize.Width <= 0.0f || actualSize.Height <= 0.0f)
            {
                return;
            }

            if (RenderOffscreen(ctx) is Texture tex)
            {
                ctx.MainBatch.PushQuad(
                    Quad,
                    tex,
                    alphaMask: ctx.WhiteTexture,
                    BlendMode,
                    FilterMode
                );
            }
        }

        private Texture? RenderOffscreen(RenderContext ctx)
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
                    (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetUV(ctx);
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

        public Vector3 Point(
            World world,
            RenderContext ctx,
            NsCoordinate x, NsCoordinate y)
        {
            Size screen = ctx.DesignResolution;
            Vector3 pos = Transform.Position;
            Vector3 origin = world.Get(Parent) switch
            {
                ConstraintBox { InheritTransform: false } => Vector3.Zero,
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
                    NsAlignment.Left => 0.0f,
                    NsAlignment.Center => screen.Width / 2.0f,
                    NsAlignment.Right => screen.Width,
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
                    NsAlignment.Top => 0.0f,
                    NsAlignment.Center => screen.Height / 2.0f,
                    NsAlignment.Bottom => screen.Height,
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

        public void Move(World world, RenderContext ctx, in NsCoordinate x, in NsCoordinate y, TimeSpan duration, NsEaseFunction easeFunction)
        {
            Vector3 destination = Point(world, ctx, x, y);
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

            if (AnimPropagateFlags.HasFlag(AnimPropagateFlags.Move))
            {
                foreach (RenderItem2D child in world.Children<RenderItem2D>(this))
                {
                    child.Move(destination, duration, easeFunction);
                }
            }
            else
            {
                var childX = x is { Kind: NsCoordinateKind.Value, Value: (_, isRelative: true) }
                    ? x : new NsCoordinate(0, isRelative: true);
                var childY = y is { Kind: NsCoordinateKind.Value, Value: (_, isRelative: true) }
                    ? y : new NsCoordinate(0, isRelative: true);
                foreach (RenderItem2D child in world.Children<RenderItem2D>(this))
                {
                    child.Move(world, ctx, childX, childY, duration, easeFunction);
                }
            }
            //else if (x is { Kind: NsCoordinateKind.Value, Value: (pos: var relX, isRelative: true) }
            //    && y is { Kind: NsCoordinateKind.Value, Value: (pos: var relY, isRelative: true) })
            //{
            //
            //}
        }

        public void Move(in Vector3 destination, TimeSpan duration, NsEaseFunction easeFunction)
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
        }

        public override void Dispose()
        {
            _offscreenTarget?.Dispose();
        }
    }

    internal abstract class RenderItem3D : RenderItem
    {
        protected RenderItem3D(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }

        public override bool IsMoving => false;
    }

    internal static class RenderItemExt
    {
        public static T WithPosition<T>(
            this T renderItem,
            World world,
            RenderContext ctx,
            NsCoordinate x,
            NsCoordinate y)
            where T : RenderItem2D
        {
            renderItem.Transform.Position = renderItem.Point( world, ctx, x, y);
            return renderItem;
        }
    }
}
