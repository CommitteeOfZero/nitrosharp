using System;
using System.Numerics;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Core
{
    internal sealed class RenderTarget : IDisposable
    {
        private readonly bool _ownsFramebuffer;
        private readonly Texture _colorTarget;
        private readonly Framebuffer _framebuffer;

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
            _colorTarget = factory.CreateTexture(ref textureDesc);
            var desc = new FramebufferDescription(depthTarget: null, _colorTarget);
            _framebuffer = factory.CreateFramebuffer(ref desc);
            _ownsFramebuffer = true;
            Size = size;
            OutputDescription = _framebuffer.OutputDescription;
            ViewProjection = ViewProjection.CreateOrtho(
                graphicsDevice,
                new RectangleF(Vector2.Zero, Size)
            );
        }

        private RenderTarget(GraphicsDevice graphicsDevice, Framebuffer existingFramebuffer)
        {
            _framebuffer = existingFramebuffer;
            _colorTarget = existingFramebuffer.ColorTargets[0].Target;
            _ownsFramebuffer = false;
            Size = new Size(_colorTarget.Width, _colorTarget.Height);
            ViewProjection = ViewProjection.CreateOrtho(
                graphicsDevice,
                new RectangleF(Vector2.Zero, Size)
            );
            OutputDescription = _framebuffer.OutputDescription;
        }

        public Size Size { get; }
        public Framebuffer Framebuffer => _framebuffer;

        public Texture ColorTarget => _colorTarget;

        public ViewProjection ViewProjection { get; }
        public OutputDescription OutputDescription { get; }

        public Texture GetStagingTexture(ResourceFactory resourceFactory)
        {
            _stagingTexture ??= resourceFactory.CreateTexture(TextureDescription.Texture2D(
                _colorTarget.Width, _colorTarget.Height,
                mipLevels: 1, arrayLayers: 1,
                _colorTarget.Format, TextureUsage.Staging
            ));
            return _stagingTexture;
        }

        public void Dispose()
        {
            if (_ownsFramebuffer)
            {
                _framebuffer.Dispose();
                _colorTarget.Dispose();
            }

            _stagingTexture?.Dispose();
            ViewProjection.Dispose();
        }
    }
}
