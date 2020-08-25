namespace NitroSharp.Graphics
{
    internal sealed class Image : Entity
    {
        public Image(in ResolvedEntityPath path, SpriteTexture texture)
            : base(path)
        {
            Texture = texture;
        }

        public SpriteTexture Texture { get; }
        public override bool IsIdle => true;

        public override void Dispose()
        {
            Texture.Dispose();
        }
    }
}
