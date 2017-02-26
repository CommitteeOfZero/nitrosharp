using SciAdvNet.MediaLayer.Platform;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    internal class DXRenderContext : RenderContext
    {
        private readonly Window _window;
        private DXDrawingSession _session;

        private SharpDX.Direct2D1.Device _d2dDevice;
        private SharpDX.Direct3D11.Device1 _d3dDevice;
        private SharpDX.DXGI.Device2 _dxgiDevice;
        private SharpDX.Direct2D1.Bitmap1 _backBufferBitmap;

        internal SharpDX.Direct2D1.DeviceContext DeviceContext;
        internal SharpDX.DXGI.SwapChain1 SwapChain;
        internal SolidColorBrush ColorBrush;

        private SharpDX.Direct2D1.Factory1 _d2dFactory;
        internal SharpDX.DirectWrite.Factory1 DWriteFactory;
        internal SharpDX.WIC.ImagingFactory WicFactory;

        public DXRenderContext(Window window)
        {
            _window = window;
            Initialize();
            ResourceFactory = new DXResourceFactory(this);
        }

        private void Initialize()
        {
            CreateDeviceIndependentResources();
            CreateDeviceResources();
            CreateSizeDependentResources();

            _session = new DXDrawingSession(this);
        }

        private void CreateDeviceIndependentResources()
        {
            _d2dFactory = new SharpDX.Direct2D1.Factory1();
            DWriteFactory = new SharpDX.DirectWrite.Factory1();
            WicFactory = new SharpDX.WIC.ImagingFactory();
        }

        private void CreateDeviceResources()
        {
            SharpDX.Direct3D.FeatureLevel[] featureLevels =
            {
                SharpDX.Direct3D.FeatureLevel.Level_11_1,
                SharpDX.Direct3D.FeatureLevel.Level_11_0
            };

            using (var defaultDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, featureLevels))
            {
                _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            }

            _dxgiDevice = _d3dDevice.QueryInterface<SharpDX.DXGI.Device2>();
            _d2dDevice = new SharpDX.Direct2D1.Device(_d2dFactory, _dxgiDevice);
            DeviceContext = new SharpDX.Direct2D1.DeviceContext(_d2dDevice, new DeviceContextOptions());

            ColorBrush = new SolidColorBrush(DeviceContext, SharpDX.Color.White);
        }

        private void CreateSizeDependentResources()
        {
            var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = 0,
                Height = 0,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                BufferCount = 2,
                Usage = SharpDX.DXGI.Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Scaling = SharpDX.DXGI.Scaling.Stretch
            };

            using (var dxgiFactory = _dxgiDevice.Adapter.GetParent<SharpDX.DXGI.Factory2>())
            {
                SwapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, _d3dDevice, _window.Handle, ref swapChainDesc);
            }
            using (var backbuffer = SharpDX.DXGI.Surface.FromSwapChain(SwapChain, 0))
            {
                var pixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied);
                var bitmapProperties = new BitmapProperties1(pixelFormat, 0, 0, BitmapOptions.Target | BitmapOptions.CannotDraw);
                _backBufferBitmap = new SharpDX.Direct2D1.Bitmap1(DeviceContext, backbuffer, bitmapProperties);
                DeviceContext.Target = _backBufferBitmap;
            }

            DeviceContext.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
        }

        public override DrawingSession NewSession(Color clearColor)
        {
            _session.Reset(clearColor);
            return _session;
        }

        public override void Dispose()
        {
            ColorBrush.Dispose();
            base.Dispose();
        }
    }
}
