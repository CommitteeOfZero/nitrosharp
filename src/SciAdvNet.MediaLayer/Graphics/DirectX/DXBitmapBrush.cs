namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXBitmapBrush : BitmapBrush
    {
        public DXBitmapBrush(RenderContext renderContext, Texture2D bitmap, float opacity) : base(renderContext, bitmap)
        {
            var dxContext = renderContext as DXRenderContext;
            var dxBitmap = bitmap as DXTexture2D;

            var brushProps = new SharpDX.Direct2D1.BitmapBrushProperties1
            {
                ExtendModeX = SharpDX.Direct2D1.ExtendMode.Clamp,
                ExtendModeY = SharpDX.Direct2D1.ExtendMode.Clamp,
                InterpolationMode = SharpDX.Direct2D1.InterpolationMode.NearestNeighbor
            };

            DeviceBrush = new SharpDX.Direct2D1.BitmapBrush1(dxContext.DeviceContext, dxBitmap.D2DBitmap, brushProps);
            Opacity = opacity;
        }

        internal SharpDX.Direct2D1.BitmapBrush1 DeviceBrush { get; }

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
