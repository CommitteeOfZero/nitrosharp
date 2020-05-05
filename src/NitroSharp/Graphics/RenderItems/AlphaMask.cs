using NitroSharp.Content;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class AlphaMask :  ConstraintBox
    {
        public AlphaMask(
            in ResolvedEntityPath path,
            int priority,
            AssetRef<Texture> texture,
            bool inheritTransform)
            : base(path, priority, inheritTransform)
        {
            Texture = texture;
        }

        public AssetRef<Texture> Texture { get; }

        public override void Render(RenderContext ctx)
        {
        }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => ctx.Content.GetTextureSize(Texture);

        public override void Dispose()
        {
            base.Dispose();
            Texture.Dispose();
        }
    }
}
