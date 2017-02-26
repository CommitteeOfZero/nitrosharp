using ProjectHoppy.Graphics;
using System.IO;

namespace ProjectHoppy.Content
{
    public class TextureLoader : ContentLoader
    {
        private readonly GraphicsSystem _graphics;

        public TextureLoader(GraphicsSystem graphics)
        {
            _graphics = graphics;
        }

        public override object Load(Stream stream)
        {
            return _graphics.RenderContext.ResourceFactory.CreateTexture(stream);
        }
    }
}
