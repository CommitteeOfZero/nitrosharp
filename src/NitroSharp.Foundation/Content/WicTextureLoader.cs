using NitroSharp.Foundation.Graphics;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.IO;

namespace NitroSharp.Foundation.Content
{
    public class WicTextureLoader : ContentLoader
    {
        private readonly DxRenderContext _rc;

        public WicTextureLoader(DxRenderContext dxRenderContext)
        {
            _rc = dxRenderContext;
        }

        public override object Load(Stream stream)
        {
            using (stream)
            {
                var decoder = new BitmapDecoder(_rc.WicFactory, stream, DecodeOptions.CacheOnDemand);
                using (var pixelFormatConverter = new FormatConverter(_rc.WicFactory))
                using (var frame = decoder.GetFrame(0))
                {
                    pixelFormatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPBGRA);
                    var d2dBitmapProps = new BitmapProperties1()
                    {
                        BitmapOptions = BitmapOptions.None,
                        PixelFormat = _rc.DeviceContext.PixelFormat,
                        DpiX = 96,
                        DpiY = 96
                    };

                    var bitmap = Bitmap1.FromWicBitmap(_rc.DeviceContext, pixelFormatConverter, d2dBitmapProps);
                    return new DxTexture2D(bitmap, decoder);
                }
            }
        }
    }
}
