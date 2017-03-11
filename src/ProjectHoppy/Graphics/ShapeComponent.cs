using SciAdvNet.MediaLayer;

namespace ProjectHoppy.Graphics
{
    public class ShapeComponent : Component
    {
        public ShapeComponent()
        {
        }

        public ShapeComponent(ShapeKind kind, Color fillColor)
        {
            Kind = kind;
            FillColor = fillColor;
        }

        public ShapeKind Kind { get; set; }
        public Color FillColor { get; set; }
    }

    public enum ShapeKind
    {
        Rectangle
    }
}
