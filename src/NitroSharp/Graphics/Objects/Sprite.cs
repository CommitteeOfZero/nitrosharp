using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class Sprite : Visual
    {
        private BindableTexture _texture;

        public Sprite(AssetRef<BindableTexture> source, RectangleF? sourceRectangle, float opacity, int priority)
            : base(RgbaFloat.White, opacity, priority)
        {
            Source = source;
            SourceRectangle = sourceRectangle;
            _texture = source.Asset;
            Bounds = sourceRectangle == null
                ? new SizeF(_texture.Width, _texture.Height)
                : new SizeF(SourceRectangle.Value.Width, SourceRectangle.Value.Height);
        }

        public AssetRef<BindableTexture> Source { get; set; }
        public RectangleF? SourceRectangle { get; set; }
        public override SizeF Bounds { get; }

        public override void Render(RenderContext renderContext)
        {
            var dstRect = new RectangleF(0, 0, Bounds.Width, Bounds.Height);
            renderContext.Canvas.DrawImage(Source.Asset.GetTextureView(), SourceRectangle, dstRect, Color);
        }
    }
}
