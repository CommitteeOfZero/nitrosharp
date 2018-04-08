using System;
using NitroSharp.Primitives;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;

namespace NitroSharp.Content
{
    internal sealed class WicTextureData : TextureData
    {
        private readonly BitmapDecoder _bitmapDecoder;
        private readonly BitmapFrameDecode _frameDecode;
        private readonly FormatConverter _pixelFormatConverter;
        private readonly Size2 _size;

        public WicTextureData(BitmapDecoder bitmapDecoder, BitmapFrameDecode frameDecode, FormatConverter pixelFormatConverter)
        {
            _bitmapDecoder = bitmapDecoder;
            _frameDecode = frameDecode;
            _pixelFormatConverter = pixelFormatConverter;
            _size = _pixelFormatConverter.Size;
        }

        public override Size Size => new Size((uint)_size.Width, (uint)_size.Height);

        public override void CopyPixels(IntPtr buffer)
        {
            int rowWidth = _size.Width * 4;
            _pixelFormatConverter.CopyPixels(rowWidth, buffer, rowWidth * _size.Height);
        }

        public override void CopyPixels(IntPtr buffer, uint size)
        {
            int rowWidth = _size.Width * 4;
            _pixelFormatConverter.CopyPixels(rowWidth, buffer, (int)size);
        }

        public override void CopyPixels(IntPtr buffer, in Rectangle rectangle)
        {
            int rowWidth = _size.Width * 4;
            var rect = new RawBox(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            _pixelFormatConverter.CopyPixels(rect, rowWidth, new DataPointer(buffer, rect.Height * rowWidth));
        }

        public override void Dispose()
        {
            _pixelFormatConverter.Dispose();
            _frameDecode.Dispose();
            _bitmapDecoder.Dispose();
        }
    }
}
