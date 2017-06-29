using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public class CustomTextRenderer : TextRendererBase
    {
        private readonly RenderTarget _renderTarget;
        private readonly Brush _defaultBrush;

        public CustomTextRenderer(RenderTarget renderTarget, Brush defaultBrush)
        {
            _renderTarget = renderTarget;
            _defaultBrush = defaultBrush;
        }

        public override Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY,
            MeasuringMode measuringMode, GlyphRun glyphRun, GlyphRunDescription glyphRunDescription, ComObject clientDrawingEffect)
        {
            Brush brush = _defaultBrush;
            float originalOpacity = brush.Opacity;

            var context = clientDrawingEffect as TextDrawingContext;
            if (context != null)
            {
                brush.Opacity = context.OpacityOverride;
            }

            _renderTarget.DrawGlyphRun(new Vector2(baselineOriginX, baselineOriginY), glyphRun, brush, measuringMode);
            brush.Opacity = originalOpacity;
            return Result.Ok;
        }

        public override bool IsPixelSnappingDisabled(object clientDrawingContext) => false;
    }
}
