namespace NitroSharp.Graphics;

internal sealed class Image : Entity
{
    public Image(EntityName name, Entity? parent, in SpriteTexture texture) : base(name, parent)
    {
        Texture = texture;
    }

    public SpriteTexture Texture { get; }

    public override void Dispose()
    {
        Texture.Dispose();
    }
}
