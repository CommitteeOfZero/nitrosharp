using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
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

    internal abstract class RenderItem : Entity, IComparable<RenderItem>
    {
        private static int s_lastId;
        private Transform _transform;
        private RgbaFloat _color;

        protected RenderItem(in ResolvedEntityPath path, int priority)
            : base(path)
        {
            Key = new RenderItemKey(priority, s_lastId++);
            _color = RgbaFloat.White;
            _transform = Transform.Default;
        }

        public RenderItemKey Key { get; }
        public ref RgbaFloat Color => ref _color;
        public ref Transform Transform => ref _transform;

        public virtual void LayoutPass(World world, RenderContext ctx)
        {
        }

        public virtual void Render(RenderContext ctx)
        {
        }

        public int CompareTo(RenderItem? other)
            => Key.CompareTo(other!.Key);
    }

    internal abstract class RenderItem2D : RenderItem
    {
        private RenderTarget? _offscreenTarget;
        private QuadGeometry _quad;
        private Matrix4x4 _worldMatrix;

        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(path, priority)
        {
        }

        public RectangleF BoundingRect { get; private set; }
        protected ref QuadGeometry Quad => ref _quad;
        protected ref Matrix4x4 WorldMatrix => ref _worldMatrix;

        public BlendMode BlendMode { get; set; }
        public FilterMode FilterMode { get; set; }
        public AssetRef<Texture>? AlphaMaskOpt { get; set; }

        public abstract Size GetUnconstrainedBounds(RenderContext ctx);

        protected Texture GetAlphaMask(RenderContext ctx)
        {
            return AlphaMaskOpt is AssetRef<Texture> maskRef
                ? ctx.Content.Get(maskRef)
                : ctx.WhiteTexture;
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
            if (_offscreenTarget is null || !_offscreenTarget.Size.Equals(actualSize))
            {
                _offscreenTarget?.Dispose();
                _offscreenTarget = new RenderTarget(ctx.GraphicsDevice, actualSize);
                using (DrawBatch batch = ctx.BeginBatch(_offscreenTarget, RgbaFloat.Clear))
                {
                    Vector3 translation = _worldMatrix.Translation;
                    _worldMatrix.Translation = Vector3.Zero;
                    (Quad, _) = QuadGeometry.Create(
                        BoundingRect.Size,
                        _worldMatrix,
                        uvTopLeft: Vector2.Zero,
                        uvBottomRight: Vector2.One,
                        color: Vector4.One
                    );

                    Render(ctx, batch);

                    _worldMatrix = Matrix4x4.CreateTranslation(translation);
                    (Quad, _) = QuadGeometry.Create(
                        BoundingRect.Size,
                        _worldMatrix,
                        uvTopLeft: Vector2.Zero,
                        uvBottomRight: Vector2.One,
                        Color.ToVector4()
                    );
                }
            }
            return _offscreenTarget.ColorTarget;
        }

        protected virtual void Render(RenderContext ctx, DrawBatch batch)
        {
        }

        public override void LayoutPass(World world, RenderContext ctx)
        {
            if (!HasParent)
            {
                Layout(world, ctx, constraintRect: null);
            }
        }

        private void Layout(World world, RenderContext ctx, RectangleF? constraintRect)
        {
            Size unconstrainedBounds = GetUnconstrainedBounds(ctx);
            WorldMatrix = Transform.GetMatrix(unconstrainedBounds);
            (Quad, BoundingRect) = QuadGeometry.Create(
                unconstrainedBounds.ToSizeF(),
                WorldMatrix,
                Vector2.Zero,
                Vector2.One,
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
