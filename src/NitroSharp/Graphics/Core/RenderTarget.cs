using System;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Core
{
    internal sealed class RenderTarget : IDisposable
    {
        private readonly bool _ownsFramebuffer;
        private Texture? _stagingTexture;

        public static RenderTarget Swapchain(GraphicsDevice graphicsDevice, Framebuffer framebuffer)
            => new RenderTarget(graphicsDevice, framebuffer);

        public RenderTarget(
            GraphicsDevice graphicsDevice,
            Size size,
            PixelFormat format = PixelFormat.B8_G8_R8_A8_UNorm)
        {
            var textureDesc = TextureDescription.Texture2D(
                size.Width, size.Height,
                mipLevels: 1, arrayLayers: 1,
                format,
                TextureUsage.RenderTarget | TextureUsage.Sampled
            );
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ColorTarget = factory.CreateTexture(ref textureDesc);
            var desc = new FramebufferDescription(depthTarget: null, ColorTarget);
            Framebuffer = factory.CreateFramebuffer(ref desc);
            _ownsFramebuffer = true;
            Size = size;
            OutputDescription = Framebuffer.OutputDescription;
        }

        private RenderTarget(GraphicsDevice graphicsDevice, Framebuffer existingFramebuffer)
        {
            Framebuffer = existingFramebuffer;
            ColorTarget = existingFramebuffer.ColorTargets[0].Target;
            _ownsFramebuffer = false;
            Size = new Size(ColorTarget.Width, ColorTarget.Height);
            OutputDescription = Framebuffer.OutputDescription;
        }

        public Size Size { get; }
        public Framebuffer Framebuffer { get; }
        public Texture ColorTarget { get; }
        public OutputDescription OutputDescription { get; }

        public Texture GetStagingTexture(ResourceFactory resourceFactory)
        {
            _stagingTexture ??= resourceFactory.CreateTexture(TextureDescription.Texture2D(
                ColorTarget.Width, ColorTarget.Height,
                mipLevels: 1, arrayLayers: 1,
                ColorTarget.Format, TextureUsage.Staging
            ));
            return _stagingTexture;
        }

        public void Dispose()
        {
            if (_ownsFramebuffer)
            {
                Framebuffer.Dispose();
                ColorTarget.Dispose();
            }

            _stagingTexture?.Dispose();
        }
    }
}
