using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public interface ICanvas
    {
        void SetTransform(Matrix3x2 transform);

        void DrawRectangle(RectangleVisual rectangle);
        void DrawTexture(TextureVisual texture);
        void DrawTransition(TransitionVisual transition);
        void DrawScreenshot(Screenshot screenshot);
        void DrawText(TextVisual text);

        void CaptureScreen();

        void Free(TextureVisual texture);
        void Free(TextVisual textVisual);
    }
}
