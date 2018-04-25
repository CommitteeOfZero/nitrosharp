using System.Collections.Generic;
using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics.Objects
{
    internal sealed class PageIndicator : Visual
    {
        private readonly List<AssetRef<TextureData>> _icons;
        private BindableTexture _textureArray;
        private Size _size;

        private PageIndicator(List<AssetRef<TextureData>> icons)
        {
            _icons = icons;
            IconCount = (uint)icons.Count;
            Priority = int.MaxValue;
        }

        public override SizeF Bounds => new SizeF(_size.Width, _size.Height);
        public uint IconCount { get; }
        public uint ActiveIconIndex { get; set; }

        public static PageIndicator Load(ContentManager content, string iconFolder)
        {
            var icons = new List<AssetRef<TextureData>>(32);
            foreach (string filePath in content.Search(iconFolder, "*.png"))
            {
                icons.Add(content.Get<TextureData>(filePath));
            }

            return new PageIndicator(icons);
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            var device = renderContext.Device;
            var factory = renderContext.Factory;

            _size = _icons[0].Asset.Size;
            var staging = factory.CreateTexture(TextureDescription.Texture2D(
                _size.Width, _size.Height, 1, (uint)_icons.Count,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            for (uint i = 0; i < _icons.Count; i++)
            {
                var map = device.Map(staging, MapMode.Write, i);
                var textureData = _icons[(int)i].Asset;
                textureData.CopyPixels(map.Data);
                device.Unmap(staging, i);
            }

            _textureArray = new BindableTexture(factory,
                factory.CreateTexture(TextureDescription.Texture2D(
                    _size.Width, _size.Height, 1, (uint)_icons.Count,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled)));

            var cl = factory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(staging, _textureArray);
            cl.End();

            device.SubmitCommands(cl);
            device.DisposeWhenIdle(staging);
            device.DisposeWhenIdle(cl);

            foreach (var assetRef in _icons)
            {
                assetRef.Dispose();
            }

            _icons.Clear();
        }

        public override void Render(RenderContext renderContext)
        {
            var view = _textureArray.GetTextureView(0, 1, ActiveIconIndex, 1);
            renderContext.Canvas.DrawImage(view, 0, 0, Color);
        }

        public override void Destroy(RenderContext renderContext)
        {
            _textureArray.Dispose();
        }
    }
}
