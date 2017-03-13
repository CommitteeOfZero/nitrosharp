using SciAdvNet.MediaLayer;

namespace ProjectHoppy.Graphics
{
    public enum VisualKind
    {
        Rectangle,
        Texture,
        Text
    }

    public class VisualComponent : Component
    {
        public VisualComponent()
        {
        }

        public VisualComponent(VisualKind kind, float x, float y, float width, float height, int layerDepth)
        {
            Kind = kind;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            LayerDepth = layerDepth;
        }

        public VisualKind Kind { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public RgbaValueF Color { get; set; }
        public float Opacity { get; set; } = 1.0f;
        public int LayerDepth { get; set; }
    }
}
