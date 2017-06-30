using CommitteeOfZero.NitroSharp.Foundation.Graphics;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.IO;

namespace CommitteeOfZero.NitroSharp.Foundation.Content
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
            using (var bitmapDecoder = new BitmapDecoder(_rc.WicFactory, stream, DecodeOptions.CacheOnDemand))
            {
                using (var frameDecode = bitmapDecoder.GetFrame(0))
                using (var converter = new FormatConverter(_rc.WicFactory))
                {
                    converter.Initialize(frameDecode, SharpDX.WIC.PixelFormat.Format32bppPBGRA);
                    var props = new BitmapProperties1()
                    {
                        BitmapOptions = BitmapOptions.None,
                        PixelFormat = _rc.DeviceContext.PixelFormat,
                        DpiX = 96,
                        DpiY = 96
                    };

                    var bitmap = Bitmap1.FromWicBitmap(_rc.DeviceContext, converter, props);
                    return new DxTexture2D(bitmap);
                }
            }
        }
    }
}
