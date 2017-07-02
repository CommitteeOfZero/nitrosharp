using SharpDX.Direct2D1;
using SharpDX.WIC;
using System.Drawing;

namespace NitroSharp.Foundation.Graphics
{
    public sealed class DxTexture2D : Texture2D
    {
        private readonly SharpDX.WIC.BitmapDecoder _wicDecoder;

        public DxTexture2D(Bitmap1 d2dBitmap, BitmapDecoder wicDecoder)
        {
            D2DBitmap = d2dBitmap;
            _wicDecoder = wicDecoder;
        }

        public DxTexture2D(Bitmap1 d2dBitmap) : this(d2dBitmap, null)
        {
        }

        public Bitmap1 D2DBitmap { get; }
        public override SizeF Size => new SizeF(D2DBitmap.Size.Width, D2DBitmap.Size.Height);

        public override void CopyFrom(Texture2D source)
        {
            var sourceBitmap = (source as DxTexture2D).D2DBitmap;
            D2DBitmap.CopyFromBitmap(sourceBitmap);
        }

        public override void Dispose()
        {
            D2DBitmap.Dispose();
            _wicDecoder?.Dispose();
        }
    }
}
