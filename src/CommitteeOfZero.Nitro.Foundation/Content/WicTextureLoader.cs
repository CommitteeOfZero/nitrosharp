using CommitteeOfZero.Nitro.Foundation.Graphics;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.IO;
using System;
using System.Linq;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public class WicTextureLoader : ContentLoader
    {
        private readonly DxRenderContext _rc;

        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        private static readonly byte[] JpegMagic1 = { 0xFF, 0xD8, 0xFF, 0xDB };
        private static readonly byte[] JpegMagic2 = { 0xFF, 0xD8, 0xFF, 0xE0 };

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

                    var bitmap = SharpDX.Direct2D1.Bitmap1.FromWicBitmap(_rc.DeviceContext, converter, props);
                    var size = bitmap.PixelSize;
                    return new TextureAsset(bitmap);
                }
            }
        }

        public override bool IsSupportedContentType(BinaryReader reader)
        {
            var header = reader.ReadBytes(PngMagic.Length);
            return header.SequenceEqual(PngMagic) || IsJpeg(header);
        }

        private bool IsJpeg(byte[] header)
        {
            var fourBytes = header.Take(4);
            return fourBytes.SequenceEqual(JpegMagic1) || fourBytes.SequenceEqual(JpegMagic2);
        }
    }
}
