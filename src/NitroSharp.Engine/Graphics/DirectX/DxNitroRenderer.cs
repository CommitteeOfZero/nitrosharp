using NitroSharp.Foundation.Graphics;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Numerics;
using SharpDX.Direct2D1.Effects;
using System.Drawing;
using System;
using System.Collections.Generic;

namespace NitroSharp.Graphics
{
    public sealed partial class DxNitroRenderer : INitroRenderer
    {
        private readonly InterpolationMode DefInterpolationMode = InterpolationMode.Cubic;

        private DxRenderContext _rc;
        private readonly System.Drawing.Size _designResolution;

        private Flood _floodEffect;
        private Effect<FadeMaskEffect> _fadeMaskEffect;

        private Texture2D _renderTarget;
        public Texture2D Target
        {
            get => _renderTarget;
            set
            {
                _rc.DeviceContext.Target = (value as DxTexture2D).D2DBitmap;
                _renderTarget = value;
            }
        }

        public event EventHandler BackBufferResized;

        public Texture2D BackBuffer { get; private set; }

        public DxNitroRenderer(DxRenderContext renderContext, System.Drawing.Size designResolution, IEnumerable<string> userFontLocations)
        {
            _rc = renderContext;
            _designResolution = designResolution;

            _rc.D2DFactory.RegisterEffect<FadeMaskEffect>();
            _floodEffect = new Flood(_rc.DeviceContext);
            _fadeMaskEffect = new Effect<FadeMaskEffect>(_rc.DeviceContext);

            BackBuffer = new DxTexture2D(_rc.BackBufferBitmap);
            renderContext.SwapChainResized += OnSwapChainResized;
            CreateTextResources(userFontLocations);
        }

        private void OnSwapChainResized(object sender, System.EventArgs e)
        {
            BackBuffer = new DxTexture2D(_rc.BackBufferBitmap);
            BackBufferResized?.Invoke(this, EventArgs.Empty);
        }

        public Texture2D CreateRenderTarget(SizeF sizeInDip)
        {
            var properties = new BitmapProperties1(_rc.PixelFormat, _rc.CurrentDpi.Width, _rc.CurrentDpi.Height, BitmapOptions.Target);
            var dpi = _rc.CurrentDpi;
            var sizeInPx = new SharpDX.Size2((int)(sizeInDip.Width * dpi.Width / 96.0f), (int)(sizeInDip.Height * dpi.Height / 96.0f));
            var bitmap = new Bitmap1(_rc.DeviceContext, sizeInPx, properties);
            return new DxTexture2D(bitmap, null);
        }

        public void Draw(Texture2D texture)
        {
            var dxTexture = texture as DxTexture2D;
            var dst = new SharpDX.RectangleF(0, 0, texture.Size.Width, texture.Size.Height);
            _rc.DeviceContext.DrawBitmap(dxTexture.D2DBitmap, dst, 1.0f, DefInterpolationMode, null, null);
        }

        public void Draw(Texture2D texture, RectangleF destinationRectangle)
        {
            var dxTexture = texture as DxTexture2D;
            var dst = new SharpDX.RectangleF(destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height);
            _rc.DeviceContext.DrawBitmap(dxTexture.D2DBitmap, dst, 1.0f, DefInterpolationMode, null, null);
        }

        public void DrawRectangle(RectangleVisual rectangle)
        {
            _rc.ColorBrush.Color = rectangle.Color;
            _rc.ColorBrush.Opacity = rectangle.Opacity;

            var dest = new SharpDX.RectangleF(0, 0, rectangle.Width, rectangle.Height);
            _rc.DeviceContext.FillRectangle(dest, _rc.ColorBrush);
        }

        public void DrawSprite(Sprite sprite)
        {
            var target = _rc.DeviceContext;
            var dxTexture = sprite.Source.Asset as DxTexture2D;
            if (sprite.SourceRectangle == null)
            {
                var dst = new SharpDX.RectangleF(0, 0, dxTexture.Size.Width, dxTexture.Size.Height);
                target.DrawBitmap(dxTexture.D2DBitmap, dst, sprite.Opacity, DefInterpolationMode, null, null);
            }
            else
            {
                var drawingRect = sprite.SourceRectangle.Value;
                var srcRect = new SharpDX.RectangleF(drawingRect.X, drawingRect.Y, drawingRect.Width, drawingRect.Height);
                var dst = new SharpDX.RectangleF(0, 0, sprite.Measure().Width, sprite.Measure().Height);
                target.DrawBitmap(dxTexture.D2DBitmap, dst, sprite.Opacity, DefInterpolationMode, srcRect, null);
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

        public void Free(FadeTransition transition)
        {
            var inputone = _fadeMaskEffect.GetInput(0);
            var inputtwo = _fadeMaskEffect.GetInput(1);

            inputone.Dispose();
            inputtwo.Dispose();

            _fadeMaskEffect.SetInput(0, null, true);
            _fadeMaskEffect.SetInput(1, null, true);
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

                var input0 = (imageSource.Source.Asset as DxTexture2D).D2DBitmap;
                _fadeMaskEffect.SetInput(0, input0, true);
            }

            var input1 = (transition.Mask.Asset as DxTexture2D).D2DBitmap;
            _fadeMaskEffect.SetInput(1, input1, true);
        }

        public void SetTransform(Matrix3x2 transform)
        {
            _rc.DeviceContext.Transform = new RawMatrix3x2(transform.M11, transform.M12, transform.M21, transform.M22, transform.M31, transform.M32);
        }

        public void Dispose()
        {
            _rc.DWriteFactory.UnregisterFontCollectionLoader(_userFontLoader);
            _rc.DWriteFactory.UnregisterFontFileLoader(_userFontLoader);

            _userFontCollection.Dispose();
            _userFontLoader.Dispose();
            _floodEffect.Dispose();
            _fadeMaskEffect.Dispose();
        }
    }
}
