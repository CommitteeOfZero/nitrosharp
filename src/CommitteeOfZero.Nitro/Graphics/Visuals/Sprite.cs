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
            var deviceTexture = Source.Get<TextureAsset>();
            if (SourceRectangle != null)
            {
                return new SizeF(SourceRectangle.Value.Width, SourceRectangle.Value.Height);
            }

            return new SizeF(deviceTexture.Width, deviceTexture.Height);

        }

        public override void Free(ICanvas canvas)
        {
        }

        public override void OnRemoved()
        {
            Source.Release();
        }
    }
}
