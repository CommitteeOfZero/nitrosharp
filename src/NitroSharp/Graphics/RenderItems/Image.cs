using NitroSharp.Content;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Image : Entity
    {
        public Image(in ResolvedEntityPath path, AssetRef<Texture> texture)
            : base(path)
        {
            Texture = texture;
        }

        public AssetRef<Texture> Texture { get; }
    }
}
