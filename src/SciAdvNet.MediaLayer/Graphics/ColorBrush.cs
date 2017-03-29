namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class ColorBrush : Brush
    {
        protected ColorBrush(RenderContext renderContext, RgbaValueF color, float opacity)
            : base(renderContext)
        {
        }

        public abstract RgbaValueF Color { get; set; }
    }
}
