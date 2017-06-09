using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public interface INitroRenderer : IDisposable
    {
        void SetTransform(Matrix3x2 transform);

        void DrawRectangle(RectangleVisual rectangle);
        void DrawSprite(Sprite texture);
        void DrawTransition(FadeTransition transition);
        void DrawScreenshot(Screenshot screenshot);
        void DrawText(TextVisual text);

        void CaptureScreen();

        void Free(TextVisual textVisual);
    }
}
