namespace ProjectHoppy.Graphics
{
    public class VisualComponent : Component
    {
        public VisualComponent()
        {
        }

        public VisualComponent(float x, float y, float width, float height, int layerDepth)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            LayerDepth = layerDepth;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Opacity { get; set; } = 1.0f;
        public int LayerDepth { get; set; }
    }
}
