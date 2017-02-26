using SciAdvNet.MediaLayer.Graphics;
using System.Drawing;
using System;

namespace ProjectHoppy.Graphics
{
    public class TextureVisual : RenderItem
    {
        private readonly Texture2D _texture;

        public TextureVisual(Texture2D texture, int x, int y, int width, int height, int layerDepth)
            : base(x, y, width, height, layerDepth)
        {
            _texture = texture;
        }

        public override void Render(DrawingSession drawingSession)
        {
            drawingSession.DrawTexture(_texture, new Rectangle(X, Y, Width, Height), Opacity);
        }

        public override void Dispose()
        {
            _texture.Dispose();
        }
    }
}
