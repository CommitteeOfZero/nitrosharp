using System;
using System.IO;
using System.Numerics;
using NitroSharp.Graphics;
using Veldrid;
using Veldrid.ImageSharp;

namespace CowsHead
{
    public class Head : SampleApplication
    {
        private const int Width = 1280;
        private const int Height = 720;

        private CommandList _cl;
        private BindableTexture _azis;
        private BindableTexture _meow;
        private Canvas _canvas;
        private BindableTexture _mask;

        protected override void CreateResources(ResourceFactory factory)
        {
            _cl = factory.CreateCommandList();
            var azis = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "0.jpg"), false);
            _azis = new BindableTexture(factory, azis.CreateDeviceTexture(_gd, factory));

            var meow = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "meow.png"), false);
            _meow = new BindableTexture(factory, meow.CreateDeviceTexture(_gd, factory));

            var mask = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "mask.png"), false);
            _mask = new BindableTexture(factory, mask.CreateDeviceTexture(_gd, factory));

            //var bg = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "bg.jpg"), false);
            //_bg = new BindableTexture(factory, bg.CreateDeviceTexture(_gd, factory));

            _canvas = new Canvas(_gd);
        }

        protected override void Draw(float deltaSeconds)
        {
            _cl.Begin();

            _cl.SetFramebuffer(_gd.SwapchainFramebuffer);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            var viewport = new Viewport(0, 0, _window.Width, _window.Height, 0, 0);
            //_primitiveBatch.Begin(_cl);

            //_fadeMask.Source = _azis.GetTextureView();
            //_fadeMask.Mask = _mask.GetTextureView();
            //_fadeMask.Sampler = _gd.LinearSampler;
            //_fadeMask.Projection = Matrix4x4.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1);
            //_fadeMask.Properties = new FadeMaskProperties { FadeAmount = 0.3f, Feather = 0.1f };

            //_primitiveBatch.DrawQuad(new RectangleF(0, 0, 1280, 720), _fadeMask);

            //_primitiveBatch.End();

            _canvas.Begin(_cl, viewport);
            _canvas.DrawImage(_azis, 0, 0, 1280, 720);
            _canvas.FillRectangle(0, 0, 100, 100, new RgbaFloat(0.0f, 1.0f, 0.0f, 0.5f));
            _canvas.End();

            _cl.End();
            _gd.SubmitCommands(_cl);

            _gd.SwapBuffers();
        }

        protected override void DestroyResources()
        {
            _canvas.Dispose();
            //_primitiveBatch.Dispose();
        }
    }
}
