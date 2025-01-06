namespace NitroSharp.Graphics;

internal sealed class TextBlock : RenderItem
{
    public TextBlock(EntityName name, Entity? parent, int priority, string markup, TextRenderContext textRenderContext)
        : base(name, parent, priority)
    {

    }

    public override DesignSize GetSize(RenderContext ctx)
    {
        throw new System.NotImplementedException();
    }
}
