using System.IO;

namespace ProjectHoppy.Content
{
    public class TextureLoader : ContentLoader
    {
        private readonly SciAdvNet.MediaLayer.Graphics.ResourceFactory _resourceFactory;

        public TextureLoader(SciAdvNet.MediaLayer.Graphics.ResourceFactory resourceFactory)
        {
            _resourceFactory = resourceFactory;
        }

        public override object Load(Stream stream)
        {
            return _resourceFactory.CreateTexture(stream);
        }
    }
}
