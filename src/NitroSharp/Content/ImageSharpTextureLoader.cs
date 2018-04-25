using System.IO;
using NitroSharp.Graphics;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;

namespace NitroSharp.Content
{
    internal sealed class ImageSharpTextureLoader : ContentLoader
    {
        private readonly GraphicsDevice _gd;

        public ImageSharpTextureLoader(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
        }

        public override object Load(Stream stream)
        {
            using (var image = Image.Load(stream))
            {
                var imageSharpTexture = new ImageSharpTexture(image);
                return new BindableTexture(_gd.ResourceFactory, imageSharpTexture.CreateDeviceTexture(_gd, _gd.ResourceFactory));
            }
        }
    }
}
