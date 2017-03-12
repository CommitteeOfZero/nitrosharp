using SharpDX.Direct2D1;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXColorBrush : ColorBrush
    {
        public DXColorBrush(RenderContext renderContext, RgbaValueF color, float opacity)
            : base(renderContext, color, opacity)
        {
            var dxContext = renderContext as DXRenderContext;
            DeviceBrush = new SolidColorBrush(dxContext.DeviceContext, color);
            Opacity = opacity;
        }

        internal SolidColorBrush DeviceBrush { get; }

        public override RgbaValueF Color
        {
            get => DeviceBrush.Color;
            set => DeviceBrush.Color = value;
        }

        public override float Opacity
        {
            get => DeviceBrush.Opacity;
            set => DeviceBrush.Opacity = value;
        }

        public override void Dispose()
        {
            DeviceBrush.Dispose();
            base.Dispose();
        }
    }
}
