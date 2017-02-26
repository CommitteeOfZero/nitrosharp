namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class Texture2D : DeviceResource
    {
        protected Texture2D(RenderContext renderContext)
            : base(renderContext)
        {
        }

        public abstract float Width { get; }
        public abstract float Height { get; }
    }
}
