using System.IO;
using NitroSharp.Graphics;
using SharpDX.WIC;
using Veldrid;

namespace NitroSharp.Content
{
    public class WicTextureLoader : ContentLoader
    {
        private readonly GraphicsDevice _gd;
        private readonly ImagingFactory WicFactory;

        public WicTextureLoader(GraphicsDevice gd)
        {
            _gd = gd;
            WicFactory = new SharpDX.WIC.ImagingFactory();
        }

        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint MipLevels { get; private set; }

        public override object Load(Stream stream)
        {
            using (stream)
            {
                var decoder = new BitmapDecoder(WicFactory, stream, DecodeOptions.CacheOnDemand);
                using (var pixelFormatConverter = new FormatConverter(WicFactory))
                using (var frame = decoder.GetFrame(0))
                {
                    pixelFormatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA);

                    Width = (uint)pixelFormatConverter.Size.Width;
                    Height = (uint)pixelFormatConverter.Size.Height;
                    MipLevels = 1;

                    var texture = CreateTextureViaStaging(_gd, _gd.ResourceFactory, pixelFormatConverter);
                    return new BindableTexture(_gd.ResourceFactory, texture);
                }
            }
        }

        private unsafe Texture CreateTextureViaStaging(GraphicsDevice gd, ResourceFactory factory, FormatConverter fc)
        {
            Texture staging = factory.CreateTexture(
                TextureDescription.Texture2D(Width, Height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            Texture ret = factory.CreateTexture(
                TextureDescription.Texture2D(Width, Height, 1, 1, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Begin();
            for (uint level = 0; level < MipLevels; level++)
            {
                MappedResource map = gd.Map(staging, MapMode.Write, level);
                int stride = 4 * (int)Width;
                int size = (int)Width * (int)Height * 4;
                fc.CopyPixels(stride, map.Data, size);
                gd.Unmap(staging, level);

                cl.CopyTexture(
                    staging, 0, 0, 0, level, 0,
                    ret, 0, 0, 0, level, 0,
                    Width, Height, 1, 1);

            }
            cl.End();

            gd.SubmitCommands(cl);
            gd.DisposeWhenIdle(staging);
            gd.DisposeWhenIdle(cl);

            return ret;
        }
    }
}