using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Primitives;
using NitroSharp.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;
using System.Runtime.InteropServices;

namespace NitroSharp
{
    public partial class Game : IDisposable
    {
        private bool UseWicOnWindows = false;

        private readonly Stopwatch _gameTimer;
        private readonly Configuration _configuration;
        private readonly CancellationTokenSource _shutdownCancellation;

        private readonly GameWindow _window;
        private volatile bool _needsResize;
        private volatile bool _surfaceDestroyed;
        private GraphicsDevice _graphicsDevice;
        private TexturePool _texturePool;
        private Swapchain _swapchain;
        private readonly TaskCompletionSource<int> _initializingGraphics;

        private SharpDX.WIC.ImagingFactory _wicFactory;
        private AudioDevice _audioDevice;
        private AudioSourcePool _audioSourcePool;

        private World _presenterWorld;
        private World _scriptingWorld;

        private Presenter _presenter;
        private ScriptRunner _scriptRunner;

        public Game(GameWindow window, Configuration configuration)
        {
            _shutdownCancellation = new CancellationTokenSource();
            _initializingGraphics = new TaskCompletionSource<int>();

            _configuration = configuration;
            _window = window;
            _window.Mobile_SurfaceCreated += OnSurfaceCreated;
            _window.Resized += OnWindowResized;
            _window.Mobile_SurfaceDestroyed += OnSurfaceDestroyed;

            _gameTimer = new Stopwatch();
            _presenterWorld = _scriptingWorld = new World(isPrimary: true);
            if (_configuration.UseDedicatedInterpreterThread)
            {
                _scriptingWorld = new World(isPrimary: false);
            }
        }

        internal ContentManager Content { get; private set; }
        internal FontService FontService { get; private set; }
        internal AudioDevice AudioDevice => _audioDevice;
        internal AudioSourcePool AudioSourcePool => _audioSourcePool;

        public async Task Run(bool useDedicatedThread = false)
        {
            // Blocking the main thread here so that the main loop wouldn't get executed
            // on a thread pool thread.
            // Reasoning: desktop platforms require it to be executed on the main thread.
            try
            {
                Initialize().Wait();
            }
            catch (AggregateException aex)
            {
                throw aex.Flatten();
            }
            await RunMainLoop(useDedicatedThread);
        }

        private async Task Initialize()
        {
            var initializeAudio = Task.Run((Action)SetupAudio);
            await Task.WhenAll(new[] { _initializingGraphics.Task, initializeAudio });
            CreateServices();
            _scriptRunner = new ScriptRunner(this, _scriptingWorld);
            var loadScriptTask = Task.Run((Action)_scriptRunner.LoadStartupScript);
            _presenter = new Presenter(this, _presenterWorld);
            await loadScriptTask;
            _scriptRunner.StartInterpreter();
        }

        private Task RunMainLoop(bool useDedicatedThread = false)
        {
            if (useDedicatedThread)
            {
                return Task.Factory.StartNew((Action)MainLoop, TaskCreationOptions.LongRunning);
            }
            else
            {
                try
                {
                    MainLoop();
                }
                catch (TaskCanceledException)
                {
                }

                return Task.FromResult(0);
            }
        }

        private void MainLoop()
        {
            _gameTimer.Start();

            float prevFrameTicks = 0.0f;
            while (!_shutdownCancellation.IsCancellationRequested && _window.Exists)
            {
                if (_surfaceDestroyed)
                {
                    HandleSurfaceDestroyed();
                    return;
                }
                if (_needsResize)
                {
                    Size newSize = _window.Size;
                    _swapchain.Resize(newSize.Width, newSize.Height);
                    _needsResize = false;
                }

                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                try
                {
                    Tick(deltaMilliseconds);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private void Tick(float deltaMilliseconds)
        {
            var scriptRunnerStatus = _scriptRunner.Tick();
            switch (scriptRunnerStatus)
            {
                case ScriptRunner.Status.AwaitingPresenterState:
                    _scriptRunner.SyncTo(_presenter);
                    _scriptRunner.Resume();
                    break;
                case ScriptRunner.Status.NewStateReady:
                    _presenter.SyncTo(_scriptRunner);
                    _scriptRunner.Resume();
                    break;
                case ScriptRunner.Status.Running:
                default:
                    break;
            }

            _presenter.Tick(deltaMilliseconds);
        }

        private void OnSurfaceCreated(SwapchainSource swapchainSource)
        {
            bool resuming = _initializingGraphics.Task.IsCompleted;
            SetupGraphics(swapchainSource);
            _initializingGraphics.TrySetResult(0);

            if (resuming)
            {
                if (_graphicsDevice.BackendType == GraphicsBackend.OpenGLES)
                {
                    // TODO (Android): recreate all device resources
                }

                RunMainLoop(true);
            }
        }

        private void SetupGraphics(SwapchainSource swapchainSource)
        {
            var options = new GraphicsDeviceOptions(false, null, _configuration.EnableVSync);
#if DEBUG
            options.Debug = true;
#endif
            GraphicsBackend backend = _configuration.PreferredGraphicsBackend ?? VeldridStartup.GetPlatformDefaultBackend();
            var swapchainDesc = new SwapchainDescription(swapchainSource,
                    (uint)_configuration.WindowWidth, (uint)_configuration.WindowHeight,
                    options.SwapchainDepthFormat, options.SyncToVerticalBlank);

            if (backend == GraphicsBackend.OpenGLES || backend == GraphicsBackend.OpenGL)
            {
                _graphicsDevice = _window is DesktopWindow desktopWindow
                    ? VeldridStartup.CreateDefaultOpenGLGraphicsDevice(options, desktopWindow.SdlWindow, backend)
                    : GraphicsDevice.CreateOpenGLES(options, swapchainDesc);
                _swapchain = _graphicsDevice.MainSwapchain;
            }
            else
            {
                if (_graphicsDevice == null)
                {
                    _graphicsDevice = CreateDeviceWithoutSwapchain(backend, options);
                }
                _swapchain = _graphicsDevice.ResourceFactory.CreateSwapchain(ref swapchainDesc);
            }
        }

        private GraphicsDevice CreateDeviceWithoutSwapchain(GraphicsBackend backend, GraphicsDeviceOptions options)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
                    return GraphicsDevice.CreateD3D11(options);
                case GraphicsBackend.Vulkan:
                    return GraphicsDevice.CreateVulkan(options);
                case GraphicsBackend.Metal:
                    return GraphicsDevice.CreateMetal(options);
                default:
                    return null;
            }
            _texturePool = new TexturePool(_graphicsDevice, PixelFormat.R8_G8_B8_A8_UNorm);
        }

        private void SetupAudio()
        {
            var audioParameters = AudioParameters.Default;
            var backend = _configuration.PreferredAudioBackend ?? AudioDevice.GetPlatformDefaultBackend();
            _audioDevice = AudioDevice.Create(backend, audioParameters);
            _audioSourcePool = new AudioSourcePool(_audioDevice);
        }

        private void CreateServices()
        {
            Content = CreateContentManager();
            FontService = new FontService();
            FontService.RegisterFonts(Directory.EnumerateFiles("Fonts"));
        }

        private ContentManager CreateContentManager()
        {
            TextureLoader textureLoader;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && UseWicOnWindows)
            {
                _wicFactory = new SharpDX.WIC.ImagingFactory();
                textureLoader = new WicTextureLoader(
                    _graphicsDevice,
                    _texturePool,
                    _wicFactory
                );
            }
            else
            {
                textureLoader = new FFmpegTextureLoader(_graphicsDevice, _texturePool);
            }

            var content = new ContentManager(
                _configuration.ContentRoot,
                _graphicsDevice,
                textureLoader,
                _audioDevice
            );
            return content;
        }

        private void OnWindowResized()
        {
            _needsResize = true;
        }

        private void OnSurfaceDestroyed()
        {
            _surfaceDestroyed = true;
        }

        private void HandleSurfaceDestroyed()
        {
            if (_graphicsDevice.BackendType == GraphicsBackend.OpenGLES)
            {
                // TODO (Android): destroy all device resources
                _graphicsDevice.Dispose();
                _graphicsDevice = null;
                _swapchain = null;
            }
            else
            {
                _swapchain.Dispose();
                _swapchain = null;
            }

            _surfaceDestroyed = false;
            _window.Mobile_HandledSurfaceDestroyed.Set();
        }

        public void Exit()
        {
            _shutdownCancellation.Cancel();
        }

        public void Dispose()
        {
            _graphicsDevice.WaitForIdle();
            Content.Dispose();
            FontService.Dispose();
            _texturePool.Dispose();
            _wicFactory?.Dispose();
            _swapchain.Dispose();
            _graphicsDevice.Dispose();
            _audioDevice.Dispose();
        }
    }
}
