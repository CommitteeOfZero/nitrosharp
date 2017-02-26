using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Graphics.Text;
using System.Numerics;
using SciAdvNet.MediaLayer;
using System;

namespace ProjectHoppy.Graphics
{
    public class TextVisual : RenderItem
    {
        public TextVisual(string text, int x, int y, int width, int height, int layerDepth)
            : base(x, y, width, height, layerDepth)
        {
            Text = text;
        }

        public string Text { get; }

        public override void Dispose()
        {
        }

        public override void Render(DrawingSession drawingSession)
        {
            var textformat = new TextFormat
            {
                FontFamily = "Segoe UI",
                FontSize = 20,
                FontWeight = FontWeight.Normal
            };

            var layout = drawingSession.RenderContext.ResourceFactory.CreateTextLayout(Text, textformat, 640, 140);
            drawingSession.DrawTextLayout(layout, new Vector2(50, 500), Color.White);
        }
    }
}
