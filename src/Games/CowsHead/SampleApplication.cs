using System.Diagnostics;
using NitroSharp.Utilities;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace CowsHead
{
    public abstract class SampleApplication
    {
        protected readonly Sdl2Window _window;
        protected readonly GraphicsDevice _gd;
        protected DisposeCollectorResourceFactory _factory;
        private bool _windowResized;

        public SampleApplication()
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowTitle = GetTitle(),
            };
            _window = VeldridStartup.CreateWindow(ref wci);
            _window.Resized += () =>
            {
                _windowResized = true;
                OnWindowResized();
            };
            _window.MouseMove += OnMouseMove;
            _window.KeyDown += OnKeyDown;

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(false, null, false);
#if DEBUG
            options.Debug = true;
#endif
            _gd = VeldridStartup.CreateGraphicsDevice(_window, options, GraphicsBackend.Direct3D11);
        }

        protected virtual void OnMouseMove(MouseMoveEventArgs mouseMoveEvent)
        {
        }

        protected virtual void OnKeyDown(KeyEvent keyEvent)
        {
        }

        protected virtual string GetTitle() => "Sample Text";

        public void Run()
        {
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
            CreateResources(_factory);

            Stopwatch sw = Stopwatch.StartNew();
            double previousElapsed = sw.Elapsed.TotalSeconds;

            while (_window.Exists)
            {
                double newElapsed = sw.Elapsed.TotalSeconds;
                float deltaSeconds = (float)(newElapsed - previousElapsed);

                InputSnapshot inputSnapshot = _window.PumpEvents();
                InputTracker.UpdateFrameInput(inputSnapshot);

                if (_window.Exists)
                {
                    previousElapsed = newElapsed;
                    if (_windowResized)
                    {
                        _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                        HandleWindowResize();
                    }

                    Draw(deltaSeconds);
                }
            }

            _gd.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            DestroyResources();
            _gd.Dispose();
        }


        protected abstract void CreateResources(ResourceFactory factory);
        protected virtual void DestroyResources() { }

        protected abstract void Draw(float deltaSeconds);

        protected virtual void OnWindowResized() { }

        protected virtual void HandleWindowResize() { }
    }
}
