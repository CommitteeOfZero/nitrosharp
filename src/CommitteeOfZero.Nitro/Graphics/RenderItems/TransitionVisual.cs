using MoeGame.Framework.Content;
using SharpDX.Direct2D1;
using System;
using System.Diagnostics;

namespace CommitteeOfZero.Nitro.Graphics.RenderItems
{
    public class TransitionVisual : Visual
    {
        private bool _inputsSet = false;
        private Bitmap1 _srcBitmap;

        public Visual Source { get; set; }
        public AssetRef MaskAsset { get; set; }

        public override void Render(RenderSystem renderSystem)
        {
            var content = renderSystem.Content;
            var canvas = renderSystem.RenderContext.DeviceContext;

            if (_srcBitmap == null)
            {
                if (Source is RectangleVisual rectangle)
                {
                    var sw = Stopwatch.StartNew();
                    var size = new SharpDX.Size2((int)rectangle.Width, (int)rectangle.Height);
                    var props = new BitmapProperties1(canvas.PixelFormat, 96, 96, BitmapOptions.Target);
                    _srcBitmap = new Bitmap1(canvas, size, props);

                    sw.Stop();
                    Console.WriteLine(sw.Elapsed.TotalMilliseconds);

                    var originalTarget = canvas.Target;
                    canvas.Target = _srcBitmap;
                    rectangle.Render(renderSystem);
                    canvas.Target = originalTarget;
                }
                else if (Source is TextureVisual texture)
                {
                    content.TryGetAsset<TextureAsset>(texture.AssetRef, out var srcAsset);
                    _srcBitmap = srcAsset;
                }
            }

            if (_srcBitmap != null && content.TryGetAsset<TextureAsset>(MaskAsset, out var mask))
            {
                var effect = renderSystem.CommonResources.TransitionEffect;
                if (!_inputsSet)
                {
                    effect.SetInput(0, _srcBitmap, true);
                    effect.SetInput(1, mask, true);

                    _inputsSet = true;
                }

                effect.SetValue(0, Opacity);
                canvas.DrawImage(effect);
            }
        }
    }
}
