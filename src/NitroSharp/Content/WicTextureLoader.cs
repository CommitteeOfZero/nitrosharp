using System.IO;
using NitroSharp.Graphics;
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
            TexturePool texturePool,
            ImagingFactory wicFactory)
            : base(graphicsDevice, texturePool)
        {
            _wicFactory = wicFactory;
        }

        protected override Texture LoadStaging(Stream stream)
        {
            using (var decoder = new BitmapDecoder(_wicFactory, stream, DecodeOptions.CacheOnDemand))
            using (var formatConv = new FormatConverter(_wicFactory))
            using (BitmapFrameDecode frame = decoder.GetFrame(0))
            {
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
                    uint size = rowWidth * height;
                    formatConv.CopyPixels((int)rowWidth, map.Data, (int)size);
                }
                else
                {
                    for (uint y = 0; y < height; y++)
                    {
                        byte* dstStart = (byte*)map.Data + y * map.RowPitch;
                        formatConv.CopyPixels(
                            new RawBox(0, (int)y, (int)width, 1),
                            (int)rowWidth,
                            new SharpDX.DataPointer(dstStart, (int)rowWidth)
                        );
                    }
                }

                _gd.Unmap(stagingTexture);
                return stagingTexture;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _wicFactory.Dispose();
        }
    }
}
