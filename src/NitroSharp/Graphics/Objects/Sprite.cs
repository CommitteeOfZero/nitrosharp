using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Sprite : Visual
    {
        private readonly BindableTexture _texture;

        public Sprite(AssetRef<BindableTexture> source, RectangleF? sourceRectangle, float opacity, int priority)
            : base(RgbaFloat.White, opacity, priority)
        {
            Source = source;
            SourceRectangle = sourceRectangle;
            Bounds = new SizeF(Source.Asset.Width, Source.Asset.Height);
            _texture = source.Asset;
        }

        public AssetRef<BindableTexture> Source { get; set; }
        public RectangleF? SourceRectangle { get; set; }
        public override SizeF Bounds { get; }

        public override void Render(RenderContext renderContext)
        {
            renderContext.Canvas.DrawImage(_texture.GetTextureView(), 0, 0, Opacity);
        }

        public override void Destroy(RenderContext renderContext)
        {
            Source.Dispose();
        }
    }
}
