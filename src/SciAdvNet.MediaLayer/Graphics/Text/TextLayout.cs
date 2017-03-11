using System.Drawing;

namespace SciAdvNet.MediaLayer.Graphics.Text
{
    public abstract class TextLayout : DeviceResource
    {
        protected TextLayout(RenderContext renderContext)
            : base(renderContext)
        {
        }

        public abstract string Text { get; }
        public abstract SizeF RequestedSize { get; }
        public abstract float LineSpacing { get; set; }

        public abstract void SetGlyphColor(int glyphIndex, Color color);
    }
}
