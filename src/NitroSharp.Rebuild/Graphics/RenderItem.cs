using System;
using System.Numerics;
using NitroSharp.Common;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using Veldrid;

namespace NitroSharp.Graphics;

internal readonly record struct RenderItemKey(int Priority, int Id) : IComparable<RenderItemKey>
{
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

    private readonly RenderItemKey _key;
    private RgbaFloat _color = RgbaFloat.White;
    private Transform _transform = Transform.Default;
    private OpacityAnimation? _fadeAnimation;
    private MoveAnimation? _moveAnimation;
    private ScaleAnimation? _scaleAnimation;

    protected RenderItem(EntityName name, Entity? parent, int priority) : base(name, parent)
    {
        _key = new RenderItemKey(priority, s_lastId++);
        Priority = priority;
    }

    public int Priority { get; }

    public DesignRect BoundingRect { get; private set; }

    public ref RgbaFloat Color => ref _color;
    public ref Transform Transform => ref _transform;

    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;
    public FilterMode FilterMode { get; set; } = FilterMode.Linear;

    public abstract DesignSize GetSize(RenderContext ctx);

    public override void Update(GameContext ctx)
    {
        AdvanceAnimation(ref _fadeAnimation, ctx.DeltaTime);
        AdvanceAnimation(ref _moveAnimation, ctx.DeltaTime);
        AdvanceAnimation(ref _scaleAnimation, ctx.DeltaTime);

        PerformLayout(ctx, null);
    }

    private void PerformLayout(GameContext ctx, DesignRect? constraintRect)
    {
        // if (Parent is RenderItem parent)
        // {
        //     _color.SetAlpha(parent._color.A);
        // }

        // if (this is ConstraintBox)
        // {
        //     constraintRect = BoundingRect;
        // }
    }

    protected static void AdvanceAnimation<T>(ref T? anim, float dt)
        where T : Animation
    {
        if (anim?.Update(dt) is false)
        {
            anim = null;
        }
    }

    public override bool IsAnimationActive(AnimationKind animationKind)
    {
        return animationKind switch
        {
            AnimationKind.Fade => _fadeAnimation is not null,
            AnimationKind.Move => _moveAnimation is not null,
            AnimationKind.Zoom => _scaleAnimation is not null,
            _ => false
        };
    }

    public override void Move(RenderContext ctx, in NsCoordinate x, in NsCoordinate y, TimeSpan duration, NsEaseFunction easeFunction)
    {
        if (duration > TimeSpan.Zero)
        {
            _moveAnimation = new MoveAnimation(this, Transform.Position, Point(ctx, x, y), duration, easeFunction);
        }
        else
        {
            _moveAnimation = null;
            Transform.Position = Point(ctx, x, y);
        }
    }

    public override void Fade(float dstOpacity, TimeSpan duration, NsEaseFunction easeFunction)
    {
        if (duration > TimeSpan.Zero)
        {
            _fadeAnimation = new OpacityAnimation(this, Color.A, dstOpacity, duration, easeFunction);
        }
        else
        {
            _fadeAnimation = null;
            Color.SetAlpha(dstOpacity);
        }
    }

    public override void Scale(in Vector3 dstScale, TimeSpan duration, NsEaseFunction easeFunction)
    {
        if (duration > TimeSpan.Zero)
        {
            _scaleAnimation = new ScaleAnimation(this, Transform.Scale, dstScale, duration, easeFunction);
        }
        else
        {
            _scaleAnimation = null;
            Transform.Scale = dstScale;
        }
    }

    public Vector3 Point(RenderContext ctx, NsCoordinate x, NsCoordinate y)
    {
        Vector3 pos = Transform.Position;
        DesignSizeU designResolution = ctx.DesignResolution;
        DesignSize parentBounds = Parent is RenderItem parentVisual
            ? parentVisual.GetSize(ctx)
            : designResolution.ToFloatSize();
        Vector3 origin = Parent switch
        {
            ConstraintBox { IsContainer: false } => Vector3.Zero,
            RenderItem parent => parent.Transform.Position,
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
                    NsAlignment.Center => designResolution.Width / 2.0f,
                    NsAlignment.Right => origin.X + parentBounds.Width,
                    _ => throw ThrowHelper.ArgumentInvalid(nameof(x))
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
                    NsAlignment.Center => designResolution.Height / 2.0f,
                    NsAlignment.Bottom => origin.Y + parentBounds.Height,
                    _ => throw ThrowHelper.ArgumentInvalid(nameof(y))
                },
            _ => 0.0f
        };
        var anchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);
        var size = GetSize(ctx).ToVector2();
        // N2: actual size of a dialogue box is ignored when computing its final position.
        if (this is DialogueBox)
        {
            size = ctx.DesignResolution.ToVector2();
        }

        pos -= new Vector3(anchorPoint * size, 0);
        return pos;
    }

    public int CompareTo(RenderItem? other) => _key.CompareTo(other.NotNull()._key);
}

internal static class RenderItemExt
{
    public static T WithPosition<T>(
        this T renderItem,
        RenderContext ctx,
        NsCoordinate x,
        NsCoordinate y)
        where T : RenderItem
    {
        renderItem.Transform.Position = renderItem.Point(ctx, x, y);
        return renderItem;
    }
}
