using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Content;
using System.Drawing;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class Sprite : Visual
    {
        public Sprite(AssetRef source, RectangleF? sourceRectangle, float opacity, int priority)
            : base(RgbaValueF.White, opacity, priority)
        {
            Source = source;
            SourceRectangle = sourceRectangle;
        }

        public AssetRef Source { get; set; }
        public RectangleF? SourceRectangle { get; set; }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawSprite(this);
        }

        public override SizeF Measure()
        {
            if (Source.TryResolve<TextureAsset>(out var deviceTexture))
            {
                return new SizeF(deviceTexture.Width, deviceTexture.Height);
            }

            return SizeF.Empty;
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }
    }
}
