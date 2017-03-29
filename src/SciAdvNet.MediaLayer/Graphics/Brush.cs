namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class Brush : DeviceResource
    {
        public Brush(RenderContext renderContext) : base(renderContext)
        {
        }

        public abstract float Opacity { get; set; }
    }
}
