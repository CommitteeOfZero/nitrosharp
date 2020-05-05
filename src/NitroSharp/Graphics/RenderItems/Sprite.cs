using NitroSharp.Content;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Sprite : RenderItem2D
    {
        private readonly RectangleU? _rect;

        public Sprite(
            in ResolvedEntityPath path,
            int priority,
            AssetRef<Texture> texture,
            RectangleU? rect = null)
            : base(path, priority)
        {
            Texture = texture;
            _rect = rect;
        }

        public AssetRef<Texture> Texture { get; }

        public override Size GetUnconstrainedBounds(RenderContext ctx)
            => _rect?.Size ?? ctx.Content.GetTextureSize(Texture);

        protected override void Render(RenderContext ctx, DrawBatch drawBatch)
        {
            drawBatch.PushQuad(
                Quad,
                ctx.Content.Get(Texture),
                GetAlphaMask(ctx),
                BlendMode,
                FilterMode
            );
        }

        public override void Dispose()
        {
            base.Dispose();
            Texture.Dispose();
        }
    }
}
