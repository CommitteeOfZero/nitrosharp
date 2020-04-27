using NitroSharp.Content;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class AlphaMask : RenderItem2D
    {
        private readonly SizeF? _size;

        public AlphaMask(
            in ResolvedEntityPath path,
            int priority,
            AssetRef<Texture>? texture,
            SizeF? size,
            bool inheritTransform)
            : base(in path, priority)
        {
            _size = size;
            Texture = texture;
            InheritTransform = inheritTransform;
        }

        public AssetRef<Texture>? Texture { get; }
        public bool InheritTransform { get; }

        protected override SizeF GetUnconstrainedBounds(RenderContext ctx)
        {
            return Texture is AssetRef<Texture> tex ? ctx.Content.GetTextureSize(tex).ToSizeF() : _size.Value;
        }

        public override void Dispose()
        {
            Texture?.Dispose();
        }
    }
}
