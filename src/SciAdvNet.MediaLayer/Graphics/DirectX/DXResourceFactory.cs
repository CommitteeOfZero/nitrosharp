using System;
using System.IO;
using SciAdvNet.MediaLayer.Graphics.Text;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    internal class DXResourceFactory : ResourceFactory
    {
        private readonly DXRenderContext _renderContext;

        internal DXResourceFactory(DXRenderContext renderContext)
        {
            _renderContext = renderContext;
        }

        public override Texture2D CreateTexture(Stream stream)
        {
            return new DXTexture2D(_renderContext, stream);
        }

        public override TextLayout CreateTextLayout(string text, TextFormat format, float requestedWidth, float requestedHeight)
        {
            return new DXTextLayout(_renderContext, text, format, requestedWidth, requestedHeight);
        }

        public override ColorBrush CreateColorBrush(RgbaValueF color, float opacity)
        {
            return new DXColorBrush(_renderContext, color, opacity);
        }

        public override BitmapBrush CreateBitmapBrush(Texture2D bitmap, float opacity)
        {
            return new DXBitmapBrush(_renderContext, bitmap, opacity);
        }
    }
}
