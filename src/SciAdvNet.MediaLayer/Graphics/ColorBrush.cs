namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class ColorBrush : DeviceResource
    {
        protected ColorBrush(RenderContext renderContext, RgbaValueF color, float opacity)
            : base(renderContext)
        {
        }

        public abstract RgbaValueF Color { get; set; }
        public abstract float Opacity { get; set; }
    }
}
