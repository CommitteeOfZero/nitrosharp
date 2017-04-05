namespace ProjectHoppy.Graphics.RenderItems
{
    public class RectangleVisual : Visual
    {
        public override void Render(RenderSystem renderSystem)
        {
            var context = renderSystem.RenderContext;
            context.ColorBrush.Color = Color;
            context.ColorBrush.Opacity = Opacity;

            var dest = new SharpDX.RectangleF(X, Y, Width, Height);
            context.DeviceContext.FillRectangle(dest, context.ColorBrush);
        }
    }
}
