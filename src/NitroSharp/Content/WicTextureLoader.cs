using System.IO;
using NitroSharp.Graphics;
using NitroSharp.Primitives;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;

#nullable enable

namespace NitroSharp.Content
{
    internal sealed unsafe class WicTextureLoader : TextureLoader
    {
        private readonly ImagingFactory _wicFactory;

        public WicTextureLoader(
            GraphicsDevice graphicsDevice,
            TexturePool texturePool)
            : base(graphicsDevice, texturePool)
        {
            _wicFactory = new ImagingFactory();
        }

        protected override Texture LoadStaging(Stream stream)
        {
            using var wicStream = new WICStream(_wicFactory, stream);
            using var decoder = new BitmapDecoder(_wicFactory, wicStream, DecodeOptions.CacheOnDemand);
            using var formatConv = new FormatConverter(_wicFactory);
            // Do NOT dispose the frame as it might lead to a crash.
            // Seems like it's owned by the decoder, so hopefully there should be no leaks.
            BitmapFrameDecode frame = decoder.GetFrame(0);
            formatConv.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA);

            uint width = (uint)frame.Size.Width;
            uint height = (uint)frame.Size.Height;
            Texture stagingTexture = _rf.CreateTexture(TextureDescription.Texture2D(
                width, height, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            ));

            MappedResource map = _gd.Map(stagingTexture, MapMode.Write);
            uint rowWidth = width * 4;
            if (rowWidth == map.RowPitch)
            {
                formatConv.CopyPixels((int)map.RowPitch, map.Data, (int)map.SizeInBytes);
            }
            else
            {
                for (uint y = 0; y < height; y++)
                {
                    byte* dstStart = (byte*)map.Data + y * map.RowPitch;
                    formatConv.CopyPixels(
                        new RawBox(x: 0, (int)y, (int)width, height: 1),
                        (int)map.RowPitch,
                        new SharpDX.DataPointer(dstStart, (int)map.RowPitch)
                    );
                }
            }

            _gd.Unmap(stagingTexture);
            return stagingTexture;
        }

        public override void Dispose()
        {
            base.Dispose();
            _wicFactory.Dispose();
        }

        public override Size GetTextureDimensions(Stream stream)
        {
            using var wicStream = new WICStream(_wicFactory, stream);
            using var decoder = new BitmapDecoder(_wicFactory, wicStream, DecodeOptions.CacheOnDemand);
            // Do NOT dispose the frame as it might lead to a crash.
            // Seems like it's owned by the decoder, so hopefully there should be no leaks.
            BitmapFrameDecode frame = decoder.GetFrame(0);
            stream.Seek(0, SeekOrigin.Begin);
            return new Size((uint)frame.Size.Width, (uint)frame.Size.Height);
        }
    }
}
