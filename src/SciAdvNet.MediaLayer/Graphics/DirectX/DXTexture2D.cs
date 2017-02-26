using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.IO;
using System;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXTexture2D : Texture2D
    {
        private readonly DXRenderContext _rc;

        internal DXTexture2D(DXRenderContext renderContext, Stream stream)
            : base(renderContext)
        {
            _rc = renderContext;
            D2DBitmap = DecodeTexture(stream);
            Width = D2DBitmap.Size.Width;
            Height = D2DBitmap.Size.Height;
        }

        public override float Width { get; }
        public override float Height { get; }
        internal Bitmap1 D2DBitmap { get; }

        public Bitmap1 DecodeTexture(Stream stream)
        {
            using (stream)
            using (var memoryStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (var bitmapDecoder = new BitmapDecoder(_rc.WicFactory, memoryStream, DecodeOptions.CacheOnDemand))
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
                    return bitmap;
                }
            }
        }

        public override void Dispose()
        {
            D2DBitmap?.Dispose();
        }
    }
}
