using System;
using System.Numerics;
using NitroSharp.Content;
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

    interface TransitionSource
    {
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
            _transform = new Transform(inherit: true);
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
        protected RenderItem2D(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }

        public BlendMode BlendMode { get; set; }
        public FilterMode FilterMode { get; set; }
        public AssetRef<Texture>? AlphaMaskOpt { get; set; }

        public RectangleF LayoutRect { get; private set; }
        protected QuadGeometry Quad { get; private set; }
        protected Matrix4x4 WorldMatrix { get; private set; }

        protected abstract SizeF GetUnconstrainedBounds(RenderContext ctx);



        protected Texture GetAlphaMask(RenderContext ctx)
        {
            return AlphaMaskOpt is AssetRef<Texture> maskRef
                ? ctx.Content.Get(maskRef)
                : ctx.WhiteTexture;
        }

        public override void LayoutPass(World world, RenderContext ctx)
        {
            if (!HasParent)
            {
                Layout(world, ctx, Matrix4x4.Identity, null);
            }
        }

        private void Layout(
            World world,
            RenderContext ctx,
            Matrix4x4 parentTransform,
            RectangleF? boxConstraint)
        {
            SizeF unconstrainedBounds = GetUnconstrainedBounds(ctx);
            WorldMatrix = Transform.GetMatrix(unconstrainedBounds);
            if (Transform.Inherit)
            {
                WorldMatrix *= parentTransform;
            }

            (Quad, LayoutRect) = QuadGeometry.Create(
                unconstrainedBounds,
                WorldMatrix,
                Vector2.Zero,
                Vector2.One,
                Color.ToVector4(),
                boxConstraint
            );

            RectangleF? nextConstraint = this is AlphaMask ? LayoutRect : (RectangleF?)null;
            foreach (EntityId childId in Children)
            {
                if (world.Get(childId) is RenderItem2D child)
                {
                    child.Layout(world, ctx, WorldMatrix, nextConstraint);
                }
            }
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
        public static T WithPosition<T>(this T ri, RenderContext ctx, NsCoordinate x, NsCoordinate y)
            where T : RenderItem2D
        {
            Size screen = ctx.DesignResolution;
            ref Vector3 pos = ref ri.Transform.Position;
            pos.X = x switch
            {
                NsCoordinate { Kind: NsCoordinateKind.Value, Value: var val }
                    => val.isRelative ? pos.X + val.pos : val.pos,
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
                    => val.isRelative ? pos.Y + val.pos : val.pos,
                NsCoordinate { Kind: NsCoordinateKind.Alignment, Alignment: var align } => align switch
                {
                    NsAlignment.Top => 0.0f,
                    NsAlignment.Center => screen.Height / 2.0f,
                    NsAlignment.Bottom => screen.Height,
                    _ => ThrowHelper.UnexpectedValue<float>()
                },
                _ => 0.0f
            };
            ri.Transform.AnchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);
            return ri;
        }
    }
}
