using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace ProjectHoppy.Graphics.RenderItems
{
    public class ScreenCap : Visual
    {
        public void Take(RenderSystem renderSystem)
        {
            var bitmap = renderSystem.SharedResources.ScreenCapBitmap;
            bitmap.CopyFromRenderTarget(renderSystem.RenderContext.DeviceContext);
        }

        public override void Render(RenderSystem renderSystem)
        {
            var canvas = renderSystem.RenderContext.DeviceContext;
            var bitmap = renderSystem.SharedResources.ScreenCapBitmap;
            var dest = new RawRectangleF(X, Y, bitmap.Size.Width, bitmap.Size.Height);
            canvas.DrawBitmap(bitmap, dest, Opacity, BitmapInterpolationMode.Linear);
        }
    }
}
