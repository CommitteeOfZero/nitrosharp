using SharpDX.Direct2D1;
using System;
using System.Drawing;
using System.Numerics;
using SciAdvNet.MediaLayer.Graphics.Text;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXDrawingSession : DrawingSession, IDisposable
    {
        private readonly DXRenderContext _rc;
        private CustomBrushTextRenderer _txt;

        internal DXDrawingSession(DXRenderContext renderContext)
            : base(renderContext)
        {
            _rc = renderContext;
            _txt = new CustomBrushTextRenderer(_rc.DeviceContext, new SolidColorBrush(_rc.DeviceContext, SharpDX.Color.Transparent), false);
        }

        internal void Reset(Color clearColor)
        {
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(MlColorToDxColor(clearColor));
        }

        public override void DrawRectangle(System.Drawing.RectangleF rect, Color color)
        {
            _rc.ColorBrush.Color = MlColorToDxColor(color);
            _rc.DeviceContext.DrawRectangle(DrawingRectToDxRectF(rect), _rc.ColorBrush);
        }

        public override void DrawRectangle(float x, float y, float width, float height, Color color)
        {
            DrawRectangle(new RectangleF(x, y, width, height), color);
        }

        public override void FillRectangle(System.Drawing.RectangleF rect, Color color)
        {
            _rc.ColorBrush.Color = MlColorToDxColor(color);
            _rc.DeviceContext.FillRectangle(DrawingRectToDxRectF(rect), _rc.ColorBrush);
        }

        public override void FillRectangle(float x, float y, float width, float height, Color color)
        {
            FillRectangle(new RectangleF(x, y, width, height), color);
        }

        public override void DrawTexture(Texture2D texture, RectangleF destRect, float opacity)
        {
            var d2dTexture = texture as DXTexture2D;
            _rc.DeviceContext.DrawBitmap(d2dTexture.D2DBitmap, DrawingRectToDxRectF(destRect), opacity, BitmapInterpolationMode.Linear);
        }

        public override void DrawTexture(Texture2D texture, Vector2 offset, float opacity)
        {
            DrawTexture(texture, new RectangleF(offset.X, offset.Y, texture.Width, texture.Height), opacity);
        }

        public override void DrawTexture(Texture2D texture, float x, float y, float opacity)
        {
            DrawTexture(texture, new RectangleF(x, y, texture.Width, texture.Height), opacity);
        }

        public override void DrawTextLayout(TextLayout textLayout, Vector2 origin, Color color)
        {
            _rc.ColorBrush.Color = MlColorToDxColor(color);
            var dxLayout = textLayout as DXTextLayout;

            dxLayout.DWriteLayout.Draw(_txt, origin.X, origin.Y);

            //_rc.DeviceContext.DrawTextLayout(NumericsToDxVector2(origin), dxLayout.DWriteLayout, _rc.ColorBrush);
        }

        private static SharpDX.Color MlColorToDxColor(Color color) => new SharpDX.Color(color.R, color.G, color.B, color.A);
        private static SharpDX.RectangleF DrawingRectToDxRectF(System.Drawing.RectangleF rect)
        {
            return new SharpDX.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        private static SharpDX.Vector2 NumericsToDxVector2(Vector2 v) => new SharpDX.Vector2(v.X, v.Y);

        public override void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            _rc.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
