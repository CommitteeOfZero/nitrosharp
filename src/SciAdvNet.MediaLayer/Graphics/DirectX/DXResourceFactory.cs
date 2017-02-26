using System;
using System.IO;
using SciAdvNet.MediaLayer.Graphics.Text;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXResourceFactory : ResourceFactory
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
    }
}
