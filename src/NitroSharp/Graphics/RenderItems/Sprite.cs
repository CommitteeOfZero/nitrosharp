using System.Numerics;
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

        protected override bool PreciseHitTest => true;

        protected override (Vector2 uvTopLeft, Vector2 uvBottomRight) GetUV(RenderContext ctx)
        {
            if (_rect is RectangleU srcRect)
            {
                var texSize = ctx.Content.GetTextureSize(Texture).ToVector2();
                var tl = new Vector2(srcRect.Left, srcRect.Top) / texSize;
                var br = new Vector2(srcRect.Right, srcRect.Bottom) / texSize;
                return (tl, br);
            }

            return base.GetUV(ctx);
        }

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
