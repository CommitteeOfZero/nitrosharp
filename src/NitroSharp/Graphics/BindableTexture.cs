using System;
using System.Collections.Generic;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class BindableTexture : IDisposable
    {
        private readonly ResourceFactory _resourceFactory;

        private readonly Dictionary<TextureViewDescription, TextureView> _textureViews;
        private TextureViewDescription _defaultDesc;

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
            _textureViews = new Dictionary<TextureViewDescription, TextureView>();
            _defaultDesc = new TextureViewDescription(deviceTexture, 0, 1, 0, 1);
        }

        public Texture DeviceTexture { get; }
        public uint Width => DeviceTexture.Width;
        public uint Height => DeviceTexture.Height;

        private TextureView GetTextureView(ref TextureViewDescription textureViewDescription)
        {
            if (!_textureViews.TryGetValue(textureViewDescription, out var textureView))
            {
                textureView = _textureViews[textureViewDescription] =
                    _resourceFactory.CreateTextureView(ref textureViewDescription);
            }

            return textureView;
        }

        public TextureView GetTextureView(uint baseMipLevel, uint mipLevelsInView, uint baseArrayLayer, uint arrayLayersInView)
        {
            var desc = new TextureViewDescription(DeviceTexture, baseMipLevel, mipLevelsInView, baseArrayLayer, arrayLayersInView);
            return GetTextureView(ref desc);
        }

        public TextureView GetTextureView()
        {
            return GetTextureView(ref _defaultDesc);
        }

        public void Dispose()
        {
            foreach (var tv in _textureViews.Values)
            {
                tv.Dispose();
            }

            DeviceTexture.Dispose();
        }

        public static implicit operator Texture(BindableTexture bindableTexture)
        {
            return bindableTexture.DeviceTexture;
        }
    }
}
