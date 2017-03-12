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

        internal DXDrawingSession(DXRenderContext renderContext)
            : base(renderContext)
        {
            _rc = renderContext;
        }

        internal void Reset(RgbaValueF clearColor)
        {
            _rc.DeviceContext.BeginDraw();
            _rc.DeviceContext.Clear(clearColor);
        }

        public override void DrawRectangle(System.Drawing.RectangleF rect, RgbaValueF color)
        {
            _rc.ColorBrush.Color = color;
            _rc.DeviceContext.DrawRectangle(Utils.DrawingRectToDxRectF(rect), _rc.ColorBrush);
        }

        public override void DrawRectangle(float x, float y, float width, float height, RgbaValueF color)
        {
            DrawRectangle(new RectangleF(x, y, width, height), color);
        }

        public override void FillRectangle(System.Drawing.RectangleF rect, RgbaValueF color)
        {
            _rc.ColorBrush.Color = color;
            _rc.DeviceContext.FillRectangle(Utils.DrawingRectToDxRectF(rect), _rc.ColorBrush);
        }

        public override void FillRectangle(float x, float y, float width, float height, RgbaValueF color)
        {
            FillRectangle(new RectangleF(x, y, width, height), color);
        }

        public override void DrawTexture(Texture2D texture, RectangleF destRect, float opacity)
        {
            var d2dTexture = texture as DXTexture2D;
            _rc.DeviceContext.DrawBitmap(d2dTexture.D2DBitmap, Utils.DrawingRectToDxRectF(destRect), opacity, BitmapInterpolationMode.Linear);
        }

        public override void DrawTexture(Texture2D texture, Vector2 offset, float opacity)
        {
            DrawTexture(texture, new RectangleF(offset.X, offset.Y, texture.Width, texture.Height), opacity);
        }

        public override void DrawTexture(Texture2D texture, float x, float y, float opacity)
        {
            DrawTexture(texture, new RectangleF(x, y, texture.Width, texture.Height), opacity);
        }

        public override void DrawTextLayout(TextLayout textLayout, Vector2 origin, RgbaValueF color)
        {
            _rc.ColorBrush.Color = color;
            var dxLayout = textLayout as DXTextLayout;

            using (var renderer = new CustomBrushTextRenderer(_rc.DeviceContext, _rc.ColorBrush, false))
            {
                dxLayout.DWriteLayout.Draw(renderer, origin.X, origin.Y);
            }
        }

        public override void Dispose()
        {
            _rc.DeviceContext.EndDraw();
            _rc.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
