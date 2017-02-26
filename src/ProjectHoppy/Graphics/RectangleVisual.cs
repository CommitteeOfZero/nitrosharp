using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using System.Drawing;
using System;

namespace ProjectHoppy.Graphics
{
    public sealed class RectangleVisual : RenderItem
    {
        private readonly Rectangle _rect;

        public RectangleVisual(int x, int y, int width, int height, int zLevel, Color color)
            : base(x, y, width, height, zLevel)
        {
            Color = color;
            _rect = new Rectangle(X, Y, Width, Height);
        }

        public Color Color { get; }

        public override void Render(DrawingSession graphics)
        {
            graphics.FillRectangle(_rect, Color);
        }

        public override void Dispose()
        {

        }
    }
}
