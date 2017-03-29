namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class BitmapBrush : Brush
    {
        public BitmapBrush(RenderContext renderContext, Texture2D bitmap) : base(renderContext)
        {
            Bitmap = bitmap;
        }

        public Texture2D Bitmap { get; }
    }
}
