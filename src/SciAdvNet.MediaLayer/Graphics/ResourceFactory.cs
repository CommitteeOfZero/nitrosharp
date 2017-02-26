using SciAdvNet.MediaLayer.Graphics.Text;
using System.IO;

namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class ResourceFactory
    {
        public abstract Texture2D CreateTexture(Stream stream);
        public abstract TextLayout CreateTextLayout(string text, TextFormat format, float requestedWidth, float requestedHeight);
    }
}
