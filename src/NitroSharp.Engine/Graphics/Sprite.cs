using NitroSharp.Foundation;
using NitroSharp.Foundation.Content;
using NitroSharp.Foundation.Graphics;
using System.Drawing;

namespace NitroSharp.Graphics
{
    public class Sprite : Visual
    {
        private readonly SizeF _size;

        public Sprite(AssetRef<Texture2D> source, RectangleF? sourceRectangle, float opacity, int priority)
            : base(RgbaValueF.White, opacity, priority)
        {
            Source = source;
            SourceRectangle = sourceRectangle;

            var deviceTexture = source.Asset;
            _size = sourceRectangle == null ? deviceTexture.Size
                : new SizeF(SourceRectangle.Value.Width, SourceRectangle.Value.Height);
        }

        public AssetRef<Texture2D> Source { get; set; }
        public RectangleF? SourceRectangle { get; set; }

        public override void Render(INitroRenderer renderer)
        {
            renderer.DrawSprite(this);
        }

        public override SizeF Measure() => _size;

        public override void OnRemoved()
        {
            Source.Dispose();
        }
    }
}
