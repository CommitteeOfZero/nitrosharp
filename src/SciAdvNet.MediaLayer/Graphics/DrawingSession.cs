using SciAdvNet.MediaLayer.Graphics.Text;
using System;
using System.Drawing;
using System.Numerics;

namespace SciAdvNet.MediaLayer.Graphics
{
    public abstract class DrawingSession : IDisposable
    {
        internal DrawingSession(RenderContext renderContext)
        {
            RenderContext = renderContext;
        }

        public RenderContext RenderContext { get; }
        public Matrix3x2 Transform { get; set; }

        public abstract void DrawTexture(Texture2D texture, Vector2 offset, float opacity);
        public abstract void DrawTexture(Texture2D texture, float x, float y, float opacity);
        public abstract void DrawTexture(Texture2D texture, System.Drawing.RectangleF destRect, float opacity);
        public abstract void DrawRectangle(float x, float y, float width, float height, RgbaValueF color);
        public abstract void DrawRectangle(RectangleF rect, RgbaValueF color);
        public abstract void FillRectangle(RectangleF rect, RgbaValueF color);
        public abstract void FillRectangle(float x, float y, float width, float height, RgbaValueF color);

        public abstract void DrawTextLayout(TextLayout textLayout, Vector2 origin, RgbaValueF color);

        public abstract void Dispose();
    }
}
