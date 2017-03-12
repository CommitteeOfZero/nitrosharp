using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class CustomBrushTextRenderer : TextRendererBase
    {
        private readonly RenderTarget _renderTarget;
        private readonly Brush _defaultBrush;
        private readonly bool _isPixelSnappingDisabled;

        public CustomBrushTextRenderer(RenderTarget renderTarget, Brush defaultBrush, bool isPixelSnappingDisabled)
        {
            _renderTarget = renderTarget;
            _defaultBrush = defaultBrush;
            _isPixelSnappingDisabled = isPixelSnappingDisabled;
        }

        public override Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode,
            GlyphRun glyphRun, GlyphRunDescription glyphRunDescription, ComObject clientDrawingEffect)
        {
            var brush = clientDrawingEffect as Brush ?? _defaultBrush;
            _renderTarget.DrawGlyphRun(new Vector2(baselineOriginX, baselineOriginY), glyphRun, brush, measuringMode);
            return Result.Ok;
        }

        public override bool IsPixelSnappingDisabled(object clientDrawingContext)
        {
            return _isPixelSnappingDisabled;
        }

        //public static void DrawTextLayout(RenderTarget renderTarget, Vector2 origin, TextLayout textLayout, Brush defaultForegroundBrush, DrawTextOptions options = DrawTextOptions.None)
        //{
        //    if (options.HasFlag(DrawTextOptions.Clip))
        //    {
        //        renderTarget.PushAxisAlignedClip(new RectangleF(origin.X, origin.Y, textLayout.MaxWidth, textLayout.MaxHeight), renderTarget.AntialiasMode);
        //        using (var renderer = new CustomBrushTextRenderer(renderTarget, defaultForegroundBrush, options.HasFlag(DrawTextOptions.NoSnap)))
        //            textLayout.Draw(renderer, origin.X, origin.Y);
        //        renderTarget.PopAxisAlignedClip();
        //    }
        //    else
        //    {
        //        using (var renderer = new CustomBrushTextRenderer(renderTarget, defaultForegroundBrush, options.HasFlag(DrawTextOptions.NoSnap)))
        //            textLayout.Draw(renderer, origin.X, origin.Y);
        //    }
        //}
    }
}
