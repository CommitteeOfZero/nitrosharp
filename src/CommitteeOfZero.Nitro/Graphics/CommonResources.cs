using MoeGame.Framework.Graphics;
using CommitteeOfZero.Nitro.Graphics.Effects;
using SharpDX.Direct2D1;
using System;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class CommonResources : IDisposable
    {
        public CommonResources(DxRenderContext renderContext)
        {
            var canvas = renderContext.DeviceContext;
            renderContext.D2DFactory.RegisterEffect<TransitionEffect>();
            TransitionEffect = new Effect<TransitionEffect>(canvas);

            var props = new BitmapProperties1(canvas.PixelFormat, 96, 96, BitmapOptions.None);
            ScreenCapBitmap = new Bitmap1(canvas, canvas.PixelSize, props);
        }

        public Effect<TransitionEffect> TransitionEffect { get; }
        public Bitmap1 ScreenCapBitmap { get; }

        public void Dispose()
        {
            ScreenCapBitmap.Dispose();
            TransitionEffect.Dispose();
        }
    }
}
