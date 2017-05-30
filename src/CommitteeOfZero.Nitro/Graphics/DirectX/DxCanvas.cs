using CommitteeOfZero.Nitro.Foundation.Content;
using CommitteeOfZero.Nitro.Foundation.Graphics;
using SharpDX.Direct2D1;
using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed partial class DxCanvas : ICanvas
    {
        private DxRenderContext _rc;
        private ContentManager _content;

        private Bitmap1 _screenshotBitmap;
        private Effect<TransitionEffect> _transitionEffect;
        private Dictionary<int, TransitionState> _transitionState;

        public DxCanvas(DxRenderContext renderContext, ContentManager content)
        {
            _rc = renderContext;
            _content = content;
            _transitionState = new Dictionary<int, TransitionState>();

            _rc.D2DFactory.RegisterEffect<TransitionEffect>();
            _transitionEffect = new Effect<TransitionEffect>(_rc.DeviceContext);

            var props = new BitmapProperties1(_rc.DeviceContext.PixelFormat, 96, 96, BitmapOptions.None);
            _screenshotBitmap = new Bitmap1(_rc.DeviceContext, _rc.DeviceContext.PixelSize, props);

            CreateTextResources();
        }

        public Matrix3x2 Transform { get; set; }
       
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

        public void DrawScreenshot(ScreenshotVisual screenshot)
        {
            var dest = new RawRectangleF(0, 0, _screenshotBitmap.Size.Width, _screenshotBitmap.Size.Height);
            _rc.DeviceContext.DrawBitmap(_screenshotBitmap, dest, screenshot.Opacity, BitmapInterpolationMode.Linear);
        }

        public void DrawSprite(Sprite texture)
        {
            var target = _rc.DeviceContext;
            if (_content.TryGetAsset<TextureAsset>(texture.Source.Id, out var deviceTexture))
            {
                if (texture.SourceRectangle == null)
                {
                    target.DrawBitmap(deviceTexture, texture.Opacity, InterpolationMode.Anisotropic);
                }
                else
                {
                    var drawingRect = texture.SourceRectangle.Value;
                    var srcRect = new SharpDX.RectangleF(drawingRect.X, drawingRect.Y, drawingRect.Width, drawingRect.Height);
                    var dst = new SharpDX.RectangleF(0, 0, texture.Measure().Width, texture.Measure().Height);
                    target.DrawBitmap(deviceTexture, dst, texture.Opacity, InterpolationMode.Linear, srcRect, null);
                    //canvas.DrawImage(texture, new SharpDX.Vector2(0, 0), srcRect, InterpolationMode.Anisotropic, CompositeMode.SourceOver);
                }
            }
        }

        public void DrawTransition(Transition transition)
        {
            _transitionState.TryGetValue(transition.GetHashCode(), out var state);
            var srcBitmap = state?.SrcDeviceBitmap;
            var target = _rc.DeviceContext;

            if (srcBitmap == null)
            {
                if (transition.Source is RectangleVisual rectangle)
                {
                    var size = new SharpDX.Size2((int)rectangle.Width, (int)rectangle.Height);
                    var props = new BitmapProperties1(target.PixelFormat, 96, 96, BitmapOptions.Target);
                    srcBitmap = new Bitmap1(target, size, props);

                    var originalTarget = target.Target;
                    target.Target = srcBitmap;
                    DrawRectangle(rectangle);
                    target.Target = originalTarget;
                }
                else if (transition.Source is Sprite texture)
                {
                    _content.TryGetAsset<TextureAsset>(texture.Source.Id, out var srcAsset);
                    srcBitmap = srcAsset;
                }

                state = _transitionState[transition.GetHashCode()] = new TransitionState(srcBitmap);
            }

            if (srcBitmap != null && _content.TryGetAsset<TextureAsset>(transition.Mask.Id, out var mask))
            {
                if (!state.InputsSet)
                {
                    _transitionEffect.SetInput(0, srcBitmap, false);
                    _transitionEffect.SetInput(1, mask, false);

                    state.InputsSet = true;
                }
                _transitionEffect.SetValue(0, transition.Opacity);
                target.DrawImage(_transitionEffect);
            }
        }


        //public void Free(Transition transition)
        //{
        //    transition.Source.Free(this);
        //    if (_transitionState.TryGetValue(transition, out var state))
        //    {
        //        if (transition.Source is RectangleVisual)
        //        {
        //            state.SrcDeviceBitmap.Dispose();
        //        }

        //        _transitionState.Remove(transition);
        //    }
        //}

        public void SetTransform(Matrix3x2 transform)
        {
            _rc.DeviceContext.Transform = new RawMatrix3x2(transform.M11, transform.M12, transform.M21, transform.M22, transform.M31, transform.M32);
        }

        public void Dispose()
        {
            _screenshotBitmap.Dispose();
            _transitionEffect.Dispose();
        }

        private sealed class TransitionState
        {
            public TransitionState(Bitmap1 srcDeviceBitmap)
            {
                SrcDeviceBitmap = srcDeviceBitmap;
                InputsSet = false;
            }

            public Bitmap1 SrcDeviceBitmap { get; }
            public bool InputsSet { get; set; }
        }
    }
}
