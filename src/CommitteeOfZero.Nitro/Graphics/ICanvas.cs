using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public interface ICanvas : IDisposable
    {
        void SetTransform(Matrix3x2 transform);

        void DrawRectangle(RectangleVisual rectangle);
        void DrawSprite(Sprite texture);
        void DrawTransition(Transition transition);
        void DrawScreenshot(ScreenshotVisual screenshot);
        void DrawText(TextVisual text);

        void CaptureScreen();

        //void Free(Sprite texture);
        void Free(TextVisual textVisual);
        //void Free(Transition transition);
    }
}
