using NitroSharp.Foundation.Platform;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;

namespace NitroSharp.Foundation.Graphics
{
    public sealed class DxRenderContext : IDisposable
    {
        private readonly Window _window;
        private readonly bool _vsyncEnabled;
        private DxDrawingSession _session;

        private SharpDX.Direct2D1.Device _d2dDevice;
        private SharpDX.Direct2D1.Multithread _d2dLock;
        private SharpDX.Direct3D11.Device1 _d3dDevice;
        private SharpDX.DXGI.Device2 _dxgiDevice;

        private SharpDX.DXGI.Surface _dxgiBackBuffer;
        internal SharpDX.DXGI.SwapChain1 SwapChain;
        private SharpDX.DXGI.Format _displayFormat;
        private volatile bool _needsResizing;
        private System.Drawing.Size _previousWindowSize;

        public DxRenderContext(Window window, bool multithreaded, bool enableVSync)
        {
            _window = window;
            IsMultithreaded = multithreaded;
            _vsyncEnabled = enableVSync;
            Initialize();
        }

        public bool IsMultithreaded { get; }
        public SharpDX.Direct2D1.Factory1 D2DFactory { get; private set; }
        public SharpDX.DirectWrite.Factory1 DWriteFactory { get; private set; }
        public SharpDX.WIC.ImagingFactory WicFactory { get; private set; }

        public SharpDX.Direct2D1.DeviceContext DeviceContext { get; private set; }
        public SharpDX.Direct2D1.Bitmap1 BackBufferBitmap { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public SolidColorBrush ColorBrush { get; private set; }

        public Window Window => _window;
        public Size2F CurrentDpi => D2DFactory.DesktopDpi;

        public event EventHandler SwapChainResized;

        private void Initialize()
        {
#if DEBUG
            //SharpDX.Configuration.EnableObjectTracking = true;
#endif
            CreateDeviceIndependentResources();
            CreateDeviceResources();
            CreateSizeDependentResources();

            _session = new DxDrawingSession(this, _vsyncEnabled);
            _previousWindowSize = _window.Size;
            _window.Resized += OnWindowResized;
        }

        private void OnWindowResized(object sender, EventArgs e)
        {
            if (_window.Size != System.Drawing.Size.Empty && _window.Size != _previousWindowSize)
            {
                _needsResizing = true;
                _previousWindowSize = _window.Size;
            }
        }

        private void CreateDeviceIndependentResources()
        {
            var factoryType = IsMultithreaded ? FactoryType.MultiThreaded : FactoryType.SingleThreaded;
            D2DFactory = new SharpDX.Direct2D1.Factory1(factoryType);
            if (IsMultithreaded)
            {
                _d2dLock = D2DFactory.QueryInterface<SharpDX.Direct2D1.Multithread>();
            }

            DWriteFactory = new SharpDX.DirectWrite.Factory1();
            WicFactory = new SharpDX.WIC.ImagingFactory();
        }

        private void CreateDeviceResources()
        {
            SharpDX.Direct3D.FeatureLevel[] featureLevels =
            {
                SharpDX.Direct3D.FeatureLevel.Level_11_1,
                SharpDX.Direct3D.FeatureLevel.Level_11_0,
                SharpDX.Direct3D.FeatureLevel.Level_10_1,
                SharpDX.Direct3D.FeatureLevel.Level_10_0
            };

            using (var defaultDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, featureLevels))
            {
                _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            }

            _dxgiDevice = _d3dDevice.QueryInterface<SharpDX.DXGI.Device2>();
            _d2dDevice = new SharpDX.Direct2D1.Device(D2DFactory, _dxgiDevice);
            DeviceContext = new SharpDX.Direct2D1.DeviceContext(_d2dDevice, DeviceContextOptions.None);
            DeviceContext.DotsPerInch = new Size2F(CurrentDpi.Width, CurrentDpi.Height);

            DeviceContext.AntialiasMode = AntialiasMode.PerPrimitive;
            ColorBrush = new SolidColorBrush(DeviceContext, Color.CornflowerBlue);
        }

        private void CreateSizeDependentResources()
        {
            PixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied);
            _displayFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm;

            var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = 0,
                Height = 0,
                Format = _displayFormat,
                BufferCount = 2,
                Usage = SharpDX.DXGI.Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Scaling = SharpDX.DXGI.Scaling.Stretch
            };

            using (var adapter = _dxgiDevice.Adapter)
            using (var dxgiFactory = adapter.GetParent<SharpDX.DXGI.Factory2>())
            {
                SwapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, _d3dDevice, _window.Handle, ref swapChainDesc);
            }

            CreateBackBufferBitmap();

            DeviceContext.Target = BackBufferBitmap;
            DeviceContext.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
        }

        private void CreateBackBufferBitmap()
        {
            _dxgiBackBuffer = SharpDX.DXGI.Surface.FromSwapChain(SwapChain, 0);
            var bitmapProperties = new BitmapProperties1(PixelFormat, CurrentDpi.Width, CurrentDpi.Height, BitmapOptions.Target | BitmapOptions.CannotDraw);
            BackBufferBitmap = new SharpDX.Direct2D1.Bitmap1(DeviceContext, _dxgiBackBuffer, bitmapProperties);

            DeviceContext.Target = BackBufferBitmap;
        }

        private void ReleaseBackBuffer()
        {
            DeviceContext.Target = null;
            BackBufferBitmap.Dispose();
            _dxgiBackBuffer.Dispose();
        }

        private void ResizeBuffers()
        {
            ReleaseBackBuffer();
            SwapChain.ResizeBuffers(2, Window.Size.Width, Window.Size.Height, _displayFormat, SharpDX.DXGI.SwapChainFlags.None);
            CreateBackBufferBitmap();

            SwapChainResized?.Invoke(this, EventArgs.Empty);
        }

        public DxDrawingSession NewDrawingSession(RgbaValueF clearColor, bool present = true)
        {
            if (_needsResizing)
            {
                ResizeBuffers();
                _needsResizing = false;
            }

            _session.Reset(clearColor, present);
            return _session;
        }

        public void Present()
        {
            if (!IsMultithreaded)
            {
                SwapChain.Present(_vsyncEnabled ? 1 : 0, SharpDX.DXGI.PresentFlags.None);
            }
            else
            {
                _d2dLock.Enter();
                SwapChain.Present(_vsyncEnabled ? 1 : 0, SharpDX.DXGI.PresentFlags.None);
                _d2dLock.Leave();
            }
        }

        public void Dispose()
        {
            ReleaseBackBuffer();
            SwapChain.Dispose();

            ColorBrush.Dispose();
            DeviceContext.Dispose();

            _d2dDevice.Dispose();
            _dxgiDevice.Dispose();
            _d3dDevice.Dispose();

            WicFactory.Dispose();
            DWriteFactory.Dispose();
            D2DFactory.Dispose();
        }
    }
}
