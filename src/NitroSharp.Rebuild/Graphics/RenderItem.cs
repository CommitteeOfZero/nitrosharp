using System;
using System.Numerics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using Veldrid;

namespace NitroSharp.Graphics;

internal abstract class RenderItem : Entity
{
    private Transform _transform = Transform.Default;
    private RgbaFloat _color = RgbaFloat.White;
    private QuadGeometry _quad;
    private Matrix4x4 _worldMatrix;

    private OpacityAnimation? _fadeAnimation;
    private MoveAnimation? _moveAnimation;
    private ScaleAnimation? _scaleAnimation;

    protected RenderItem(EntityName name, Entity? parent, int priority) : base(name, parent)
    {
        Priority = priority;
    }

    public int Priority { get; }

    public DesignRect BoundingRect { get; private set; }

    public ref RgbaFloat Color => ref _color;
    public ref Transform Transform => ref _transform;
    protected ref QuadGeometry Quad => ref _quad;
    protected ref Matrix4x4 WorldMatrix => ref _worldMatrix;

    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;
    public FilterMode FilterMode { get; set; } = FilterMode.Linear;

    public virtual bool EnableScaling => true;

    public abstract DesignSize GetSize(RenderContext ctx);

    protected virtual (Vector2, Vector2) GetTexCoords(RenderContext ctx)
        => (Vector2.Zero, Vector2.One);

    public override void Update(GameContext ctx)
    {
        PerformLayout(ctx, null);
        _moveAnimation?.Update(ctx.DeltaTime);
        _fadeAnimation?.Update(ctx.DeltaTime);
        _scaleAnimation?.Update(ctx.DeltaTime);
    }

    private void PerformLayout(GameContext ctx, DesignRect? constraintRect)
    {
        DesignSize size = GetSize(ctx.RenderContext);
        WorldMatrix = Transform.GetMatrix(size);
        if (EnableScaling)
        {
            WorldMatrix *= Matrix4x4.CreateScale((float)ctx.RenderContext.RenderScale);
            WorldMatrix *= Matrix4x4.CreateTranslation((float)ctx.RenderContext.ViewportLeft, (float)ctx.RenderContext.ViewportTop, 0);
        }
        (Vector2 uvTopLeft, Vector2 uvBottomRight) = GetTexCoords(ctx.RenderContext);
        Quad = QuadGeometry.Create(
            size,
            WorldMatrix,
            uvTopLeft,
            uvBottomRight,
            Color.ToVector4()
        );

        // if (Parent is RenderItem parent)
        // {
        //     _color.SetAlpha(parent._color.A);
        // }

        // if (this is ConstraintBox)
        // {
        //     constraintRect = BoundingRect;
        // }
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

    public override void Fade(float dstOpacity, TimeSpan duration, NsEaseFunction easeFunction = NsEaseFunction.Linear)
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
                    NsAlignment.Center => designResolution.Height / 2.0f,
                    NsAlignment.Bottom => origin.Y + parentBounds.Height,
                    _ => ThrowHelper.UnexpectedValue<float>()
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
