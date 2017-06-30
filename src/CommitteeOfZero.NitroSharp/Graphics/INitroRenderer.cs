using CommitteeOfZero.NitroSharp.Foundation.Graphics;
using System;
using System.Drawing;
using System.Numerics;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public interface INitroRenderer : IDisposable
    {
        Texture2D Target { get; set; }
        Texture2D PrimaryRenderTarget { get; }

        void SetTransform(Matrix3x2 transform);
        Texture2D CreateRenderTarget(SizeF size);

        void Draw(Texture2D deviceTexture);
        void DrawRectangle(RectangleVisual rectangle);
        void DrawSprite(Sprite sprite);
        void DrawTransition(FadeTransition transition);
        void DrawScreenshot(Screenshot screenshot);
        void DrawText(TextVisual text);

        void CaptureScreen();

        void Free(TextVisual textVisual);
    }
}
