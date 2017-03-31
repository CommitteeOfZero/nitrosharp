using HoppyFramework.Graphics;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.IO;

namespace HoppyFramework.Content
{
    public class WicTextureLoader : ContentLoader
    {
        private readonly DXRenderContext _rc;

        public WicTextureLoader(DXRenderContext dxRenderContext)
        {
            _rc = dxRenderContext;
        }

        public override object Load(Stream stream)
        {
            using (stream)
            {
                using (var bitmapDecoder = new BitmapDecoder(_rc.WicFactory, stream, DecodeOptions.CacheOnDemand))
                {
                    var frame = bitmapDecoder.GetFrame(0);
                    var converter = new FormatConverter(_rc.WicFactory);
                    converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPBGRA);

                    var props = new BitmapProperties1()
                    {
                        BitmapOptions = BitmapOptions.Target,
                        PixelFormat = _rc.DeviceContext.PixelFormat,
                        DpiX = 96,
                        DpiY = 96
                    };

                    var bitmap = SharpDX.Direct2D1.Bitmap1.FromWicBitmap(_rc.DeviceContext, converter, props);
                    var size = bitmap.PixelSize;
                    return new TextureAsset(bitmap);
                }
            }
        }
    }
}
