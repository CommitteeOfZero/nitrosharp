using NitroSharp.Graphics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Logic.Objects
{
    internal sealed class RectangleVisual : Visual2D
    {
        private readonly RectangleF _rect;

        public RectangleVisual(float width, float height, in RgbaFloat color, float opacity, int priority)
            : base(color, opacity, priority)
        {
            _rect = new RectangleF(0, 0, width, height);
        }

        public override SizeF Bounds => new SizeF(_rect.Width, _rect.Height);

        public override void Render(Canvas canvas)
        {
            var c = new RgbaFloat(Color.R, Color.G, Color.B, Opacity);
            canvas.FillRectangle(_rect, c);
        }
    }
}
