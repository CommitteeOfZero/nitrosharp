using MoeGame.Framework.Graphics;
using SharpDX.Direct2D1;
using System;
using SharpDX.DirectWrite;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class CommonResources
    {
        public CommonResources(DxRenderContext renderContext)
        {
            CurrentGlyphBrush = new SolidColorBrush(renderContext.DeviceContext, SharpDX.Color.Transparent);
            TransparentBrush = new SolidColorBrush(renderContext.DeviceContext, SharpDX.Color.Transparent);
            CustomTextRenderer = new CustomTextRenderer(renderContext.DeviceContext, TransparentBrush, false);
        }

        public TextRenderer CustomTextRenderer { get; }
        public SolidColorBrush CurrentGlyphBrush { get; }
        public SolidColorBrush TransparentBrush { get; }
    }
}
