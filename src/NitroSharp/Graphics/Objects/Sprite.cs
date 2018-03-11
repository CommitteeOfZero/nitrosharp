using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Sprite : Visual2D
    {
        public Sprite(AssetRef<BindableTexture> source, RectangleF? sourceRectangle, float opacity, int priority)
            : base(RgbaFloat.White, opacity, priority)
        {
            Source = source;
            SourceRectangle = sourceRectangle;
        }

        public AssetRef<BindableTexture> Source { get; set; }
        public RectangleF? SourceRectangle { get; set; }

        public override SizeF Bounds => new SizeF(Source.Asset.Width, Source.Asset.Height);

        public override void Render(Canvas canvas)
        {
            canvas.DrawImage(Source.Asset, 0, 0, Opacity);
        }
    }
}
