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
        renderContext.MainBatch.PushQuad(
            Quad,
            renderContext.WhiteTexture,
            renderContext.WhiteTexture,
            default,
            BlendMode,
            FilterMode,
            null
        );
    }
}
