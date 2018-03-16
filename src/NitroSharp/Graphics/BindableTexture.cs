using System;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class BindableTexture : IDisposable
    {
        private readonly ResourceFactory _resourceFactory;
        private TextureView _textureView;

        public BindableTexture(ResourceFactory resourceFactory, Texture deviceTexture)
        {
#if DEBUG
            if (resourceFactory == null)
            {
                throw new ArgumentNullException(nameof(resourceFactory));
            }
            if (deviceTexture == null)
            {
                throw new ArgumentNullException(nameof(deviceTexture));
            }
#endif

            _resourceFactory = resourceFactory;
            DeviceTexture = deviceTexture;
        }

        public Texture DeviceTexture { get; }
        public uint Width => DeviceTexture.Width;
        public uint Height => DeviceTexture.Height;

        public TextureView GetTextureView()
        {
            if (_textureView == null)
            {
                _textureView = _resourceFactory.CreateTextureView(DeviceTexture);
            }

            return _textureView;
        }

        public void Dispose()
        {
            _textureView?.Dispose();
            DeviceTexture.Dispose();
        }
    }
}
