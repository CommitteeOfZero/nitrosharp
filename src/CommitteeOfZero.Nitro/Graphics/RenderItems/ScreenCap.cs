using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace CommitteeOfZero.Nitro.Graphics.RenderItems
{
    public class ScreenCap : Visual
    {
        public void Take(RenderSystem renderSystem)
        {
            var bitmap = renderSystem.CommonResources.ScreenCapBitmap;
            bitmap.CopyFromRenderTarget(renderSystem.RenderContext.DeviceContext);
        }

        public override void Render(RenderSystem renderSystem)
        {
            var canvas = renderSystem.RenderContext.DeviceContext;
            var bitmap = renderSystem.CommonResources.ScreenCapBitmap;
            var dest = new RawRectangleF(0, 0, bitmap.Size.Width, bitmap.Size.Height);
            canvas.DrawBitmap(bitmap, dest, Opacity, BitmapInterpolationMode.Linear);
        }
    }
}
