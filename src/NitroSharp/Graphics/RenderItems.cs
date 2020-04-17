using System;
using System.Numerics;
using NitroSharp.Content;
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

    internal class RenderItem : Entity, IComparable<RenderItem>
    {
        private static int s_lastId;
        private Transform _transform;

        public RenderItem(in ResolvedEntityPath path, int priority)
            : base(path)
        {
            Key = new RenderItemKey(priority, s_lastId++);
            _transform = new Transform(inherit: true);
        }

        public RenderItemKey Key { get; }
        public ref Transform Transform => ref _transform;

        public virtual void Update(World world, RenderContext ctx)
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
        public AssetRef<Texture>? AlphaMask { get; set; }
        public SizeF Bounds { get; protected set; }

        protected Texture GetAlphaMask(RenderContext ctx)
        {
            return AlphaMask is AssetRef<Texture> maskRef
                ? ctx.Content.Get(maskRef) ?? ctx.WhiteTexture
                : ctx.WhiteTexture;
        }

        public override void Update(World world, RenderContext ctx)
        {
            Transform.Calc(world, this);
        }
    }

    internal sealed class Sprite : RenderItem2D, TransitionSource
    {
        public Sprite(in ResolvedEntityPath path, AssetRef<Texture> texture, int priority)
            : base(in path, priority)
        {
            Texture = texture;
        }

        public AssetRef<Texture> Texture { get; }
    }

    internal sealed class ColorRect : RenderItem2D, TransitionSource
    {
        public ColorRect(in ResolvedEntityPath path, int priority, SizeF size, in RgbaFloat color)
            : base(in path, priority)
        {
            Bounds = size;
            Color = color;
        }

        public RgbaFloat Color { get; }

        public override void Render(RenderContext ctx)
        {
            ctx.PushQuad(
                QuadGeometry.Create(
                    Bounds,
                    Transform.Matrix,
                    Vector2.Zero,
                    Vector2.One,
                    Color.ToVector4(),
                    out _
                ),
                ctx.WhiteTexture,
                GetAlphaMask(ctx),
                BlendMode,
                FilterMode
            );
        }
    }
}
