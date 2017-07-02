using NitroSharp.Foundation.Graphics;
using System;
using System.Drawing;
using System.Numerics;

namespace NitroSharp.Graphics
{
    public interface INitroRenderer : IDisposable
    {
        Texture2D Target { get; set; }
        Texture2D BackBuffer { get; }

        void SetTransform(Matrix3x2 transform);
        Texture2D CreateRenderTarget(SizeF size);

        void Draw(Texture2D deviceTexture);
        void Draw(Texture2D deviceTexture, RectangleF destinationRect);
        void DrawRectangle(RectangleVisual rectangle);
        void DrawSprite(Sprite sprite);
        void DrawTransition(FadeTransition transition);
        void DrawText(TextVisual text);

        void Free(TextVisual textVisual);
        void Free(FadeTransition transition);
    }
}
