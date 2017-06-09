using CommitteeOfZero.Nitro.Foundation.Graphics;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Numerics;
using SharpDX.Direct2D1.Effects;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed partial class DxNitroRenderer : INitroRenderer
    {
        private DxRenderContext _rc;
        private readonly System.Drawing.Size _designResolution;

        private Bitmap1 _screenshotBitmap;
        private Flood _floodEffect;
        private Effect<FadeMaskEffect> _fadeMaskEffect;

        public DxNitroRenderer(DxRenderContext renderContext, System.Drawing.Size designResolution)
        {
            _rc = renderContext;
            _designResolution = designResolution;

            _rc.D2DFactory.RegisterEffect<FadeMaskEffect>();
            _floodEffect = new Flood(_rc.DeviceContext);
            _fadeMaskEffect = new Effect<FadeMaskEffect>(_rc.DeviceContext);

            var dpi = _rc.D2DFactory.DesktopDpi;
            var props = new BitmapProperties1(_rc.DeviceContext.PixelFormat, dpi.Width, dpi.Height, BitmapOptions.None);
            _screenshotBitmap = new Bitmap1(_rc.DeviceContext, _rc.DeviceContext.PixelSize, props);

            CreateTextResources();
        }

        public void CaptureScreen()
        {
            _screenshotBitmap.CopyFromRenderTarget(_rc.DeviceContext);
        }

        public void DrawRectangle(RectangleVisual rectangle)
        {
            _rc.ColorBrush.Color = rectangle.Color;
            _rc.ColorBrush.Opacity = rectangle.Opacity;

            var dest = new SharpDX.RectangleF(0, 0, rectangle.Width, rectangle.Height);
            _rc.DeviceContext.FillRectangle(dest, _rc.ColorBrush);
        }

        public void DrawScreenshot(Screenshot screenshot)
        {
            float scale = _designResolution.Width / _rc.DeviceContext.Size.Width;
            _rc.DeviceContext.Transform = SharpDX.Matrix3x2.Scaling(scale) * _rc.DeviceContext.Transform;

            var dst = new SharpDX.RectangleF(0, 0, _screenshotBitmap.Size.Width, _screenshotBitmap.Size.Height);
            _rc.DeviceContext.DrawBitmap(_screenshotBitmap, dst, screenshot.Opacity, BitmapInterpolationMode.Linear);
        }

        public void DrawSprite(Sprite sprite)
        {
            var target = _rc.DeviceContext;
            var deviceTexture = sprite.Source.Asset;
            if (sprite.SourceRectangle == null)
            {
                var dst = new SharpDX.RectangleF(0, 0, deviceTexture.Width, deviceTexture.Height);
                target.DrawBitmap(deviceTexture, dst, sprite.Opacity, InterpolationMode.Linear, null, null);
            }
            else
            {
                var drawingRect = sprite.SourceRectangle.Value;
                var srcRect = new SharpDX.RectangleF(drawingRect.X, drawingRect.Y, drawingRect.Width, drawingRect.Height);
                var dst = new SharpDX.RectangleF(0, 0, sprite.Measure().Width, sprite.Measure().Height);
                target.DrawBitmap(deviceTexture, dst, sprite.Opacity, InterpolationMode.NearestNeighbor, srcRect, null);
            }
        }

        public void DrawTransition(FadeTransition transition)
        {
            var transform = _rc.DeviceContext.Transform;
            _rc.DeviceContext.Transform = SharpDX.Matrix3x2.Scaling(_rc.CurrentDpi.Width / 96.0f) * transform;

            SetTransitionEffectInputs(transition);
            _fadeMaskEffect.SetValue(0, transition.Opacity);
            _rc.DeviceContext.DrawImage(_fadeMaskEffect);
        }

        private void SetTransitionEffectInputs(FadeTransition transition)
        {
            if (transition.TransitionSource is FadeTransition.SolidColorSource colorSource)
            {
                _floodEffect.Color = colorSource.Color;
                _fadeMaskEffect.SetInputEffect(0, _floodEffect, true);
            }
            else
            {
                var imageSource = (FadeTransition.ImageSource)transition.TransitionSource;
                _fadeMaskEffect.SetInput(0, imageSource.Source.Asset, true);
            }

            _fadeMaskEffect.SetInput(1, transition.Mask.Asset, true);
        }

        public void SetTransform(Matrix3x2 transform)
        {
            _rc.DeviceContext.Transform = new RawMatrix3x2(transform.M11, transform.M12, transform.M21, transform.M22, transform.M31, transform.M32);
        }

        public void Dispose()
        {
            _screenshotBitmap.Dispose();
            _floodEffect.Dispose();
            _fadeMaskEffect.Dispose();
        }
    }
}
