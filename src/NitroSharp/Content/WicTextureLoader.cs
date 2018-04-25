using System.IO;
using NitroSharp.Graphics;
using SharpDX.WIC;
using Veldrid;

namespace NitroSharp.Content
{
    internal sealed class WicTextureLoader : ContentLoader
    {
        private const int MipLevels = 1;

        private readonly GraphicsDevice _gd;
        private readonly ImagingFactory _wicFactory;

        public WicTextureLoader(ImagingFactory wicFactory, GraphicsDevice gd)
        {
            _gd = gd;
            _wicFactory = wicFactory;
        }

        public override object Load(Stream stream)
        {
            using (stream)
            {
                var decoder = new BitmapDecoder(_wicFactory, stream, DecodeOptions.CacheOnDemand);
                using (var pixelFormatConverter = new FormatConverter(_wicFactory))
                using (var frame = decoder.GetFrame(0))
                {
                    pixelFormatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA);
                    var texture = CreateDeviceTexture(_gd, _gd.ResourceFactory, pixelFormatConverter);
                    return new BindableTexture(_gd.ResourceFactory, texture);
                }
            }
        }

        private unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory, FormatConverter fc)
        {
            uint width = (uint)fc.Size.Width;
            uint height = (uint)fc.Size.Height;

            Texture staging = factory.CreateTexture(
                TextureDescription.Texture2D(width, height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            Texture result = factory.CreateTexture(
                TextureDescription.Texture2D(width, height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Begin();

            for (uint level = 0; level < MipLevels; level++)
            {
                MappedResource map = gd.Map(staging, MapMode.Write, level);
                uint rowWidth = width * 4;
                if (rowWidth == map.RowPitch)
                {
                    uint size = rowWidth * height;
                    fc.CopyPixels((int)rowWidth, map.Data, (int)size);
                }
                else
                {
                    for (uint y = 0; y < height; y++)
                    {
                        byte* dstStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                        fc.CopyPixels(new SharpDX.Mathematics.Interop.RawBox(0, (int)y, (int)width, 1),
                            (int)rowWidth, new SharpDX.DataPointer(dstStart, (int)rowWidth));
                    }
                }

                gd.Unmap(staging, level);
                cl.CopyTexture(
                    staging, 0, 0, 0, level, 0,
                    result, 0, 0, 0, level, 0,
                    width, height, 1, 1);
            }
            cl.End();

            gd.SubmitCommands(cl);
            gd.DisposeWhenIdle(staging);
            gd.DisposeWhenIdle(cl);

            return result;
        }
    }
}
