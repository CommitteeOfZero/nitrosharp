using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics;

internal sealed class SolidColorRect : RenderItem
{
    private readonly DesignSize _size;

    public SolidColorRect(EntityName name, Entity? parent, int priority, RgbaFloat color, DesignSize size)
        : base(name, parent, priority)
    {
        _size = size;
        Color = color;
    }

    public override DesignSize GetSize(RenderContext ctx) => _size;

    public override void Render(GameContext ctx)
    {
        RenderContext renderContext = ctx.RenderContext;

        DesignSize size = GetSize(renderContext);
        Matrix4x4 worldMatrix = Transform.GetMatrix(size);
        worldMatrix *= Matrix4x4.CreateScale((float)renderContext.RenderScale);
        worldMatrix *= Matrix4x4.CreateTranslation((float)renderContext.ViewportLeft, (float)renderContext.ViewportTop, 0);
        QuadGeometry quad = QuadGeometry.Create(
            size,
            worldMatrix,
            Vector2.Zero,
            Vector2.One,
            Color.ToVector4()
        );

        renderContext.MainBatch.PushQuad(
            quad,
            renderContext.WhiteTexture,
            renderContext.WhiteTexture,
            default,
            BlendMode,
            FilterMode,
            null
        );
    }
}
