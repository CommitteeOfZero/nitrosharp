using NitroSharp.Content;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Sprite : RenderItem2D, TransitionSource
    {
        private readonly RectangleF? _rect;

        public Sprite(
            in ResolvedEntityPath path,
            int priority,
            AssetRef<Texture> texture,
            RectangleF? rect = null)
            : base(path, priority)
        {
            Texture = texture;
            _rect = rect;
        }

        public AssetRef<Texture> Texture { get; }

        protected override SizeF GetUnconstrainedBounds(RenderContext ctx)
            => _rect?.Size ?? ctx.Content.GetTextureSize(Texture).ToSizeF();

        public override void Render(RenderContext ctx)
        {
            ctx.PushQuad(
                ctx.DrawCommands,
                Quad,
                ctx.Content.Get(Texture),
                GetAlphaMask(ctx),
                BlendMode,
                FilterMode
            );
        }

        public override void Dispose()
        {
            Texture.Dispose();
        }
    }
}
