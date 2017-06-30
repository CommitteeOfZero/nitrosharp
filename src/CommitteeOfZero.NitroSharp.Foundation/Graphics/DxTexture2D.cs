using SharpDX.Direct2D1;
using System.Drawing;

namespace CommitteeOfZero.NitroSharp.Foundation.Graphics
{
    public sealed class DxTexture2D : Texture2D
    {
        private readonly Bitmap1 _d2dBitmap;

        public DxTexture2D(object resourceHandle) : base(resourceHandle)
        {
            _d2dBitmap = resourceHandle as Bitmap1;
        }

        public override SizeF Size => new SizeF(_d2dBitmap.Size.Width, _d2dBitmap.Size.Height);

        public override void Dispose()
        {
            _d2dBitmap.Dispose();
        }
    }
}
