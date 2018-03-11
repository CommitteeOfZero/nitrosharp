using NitroSharp.Graphics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Logic.Objects
{
    internal sealed class RectangleVisual : Visual2D
    {
        private readonly RectangleF _rect;

        public RectangleVisual(in RectangleF rect)
        {
            _rect = rect;
        }

        public RectangleVisual(float width, float height, in RgbaFloat color, float opacity, int priority)
            : base(color, opacity, priority)
        {
        }

        public override SizeF Bounds => new SizeF(_rect.Width, _rect.Height);

        public override void Render(Canvas canvas)
        {
            canvas.FillRectangle(_rect, Color);
        }
    }
}
