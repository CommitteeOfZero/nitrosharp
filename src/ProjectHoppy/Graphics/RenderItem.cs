using SciAdvNet.MediaLayer.Graphics;
using System;

namespace ProjectHoppy.Graphics
{
    public abstract class RenderItem : GameObject, IDisposable
    {
        protected RenderItem(int x, int y, int width, int height, int layerDepth)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            LayerDepth = layerDepth;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; }
        public int Height { get; }
        public int LayerDepth { get; }
        public float Opacity { get; set; } = 0.0f;

        public abstract void Render(DrawingSession drawingSession);
        public abstract void Dispose();

        private Action _updateFunction;

        public virtual void Update()
        {
            _updateFunction?.Invoke();
        }

        public void FadeIn(TimeSpan duration, float finalOpacity)
        {
            Opacity = 0.0f;
            _updateFunction = () =>
            {
                if (duration.TotalMilliseconds == 0)
                {
                    Opacity = finalOpacity;
                    return;
                }

                if (Opacity < finalOpacity)
                {
                    int frameCount = (int)duration.TotalMilliseconds / 16;
                    Opacity += finalOpacity * (1.0f / frameCount);
                }
            };
        }
    }
}
