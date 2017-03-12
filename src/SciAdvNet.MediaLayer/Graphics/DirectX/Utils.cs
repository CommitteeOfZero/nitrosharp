using SharpDX;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    internal static class Utils
    {
        public static SharpDX.RectangleF DrawingRectToDxRectF(System.Drawing.RectangleF rect)
        {
            return new SharpDX.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static SharpDX.Vector2 NumericsToDxVector2(Vector2 v) => new SharpDX.Vector2(v.X, v.Y);
    }
}
