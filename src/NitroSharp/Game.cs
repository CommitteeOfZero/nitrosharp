using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;
using NitroSharp.Primitives;
using NitroSharp.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;

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
        private Swapchain _swapchain;
        private readonly TaskCompletionSource<int> _initializingGraphics;

        private VideoFrameConverter _frameConverter;
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

            _presenterWorld = _scriptingWorld = new World(WorldKind.Primary);
            World scriptingWorld = _presenterWorld;
            if (_configuration.UseDedicatedInterpreterThread)
            {
                _scriptingWorld = new World(WorldKind.Secondary);
                scriptingWorld = _scriptingWorld;
            }
        }

        internal ContentManager Content { get; private set; }
        internal FontService FontService { get; private set; }
        internal AudioDevice AudioDevice => _audioDevice;
        internal AudioSourcePool AudioSourcePool => _audioSourcePool;

        public async Task Run(bool useDedicatedThread = false)
        {
            var initializeAudio = Task.Run((Action)SetupAudio);
            await Task.WhenAll(new[] { _initializingGraphics.Task, initializeAudio });
            CreateServices();
            _scriptRunner = new ScriptRunner(this, _scriptingWorld);
            var loadScriptTask = Task.Run((Action)_scriptRunner.LoadStartupScript);
            _presenter = new Presenter(this, _presenterWorld);
            await loadScriptTask;
            _scriptRunner.StartInterpreter();
            await RunMainLoop(useDedicatedThread);
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
                    Content.SetGraphicsDevice(_graphicsDevice);
                    RecreateGraphicsResources();
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
            Content.SetGraphicsDevice(_graphicsDevice);
            FontService = new FontService();
            FontService.RegisterFonts(Directory.EnumerateFiles("Fonts"));
        }

        private ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);
            ContentLoader textureLoader = null;
            ContentLoader textureDataLoader = null;

            if (UseWicOnWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _wicFactory = new SharpDX.WIC.ImagingFactory();
                textureLoader = new WicTextureLoader(content, _wicFactory);
                textureDataLoader = new WicTextureDataLoader(content, _wicFactory);
            }
            else
            {
                textureLoader = new FFmpegTextureLoader(content);
                textureDataLoader = new FFmpegTextureDataLoader(content);
            }

            content.RegisterContentLoader(typeof(BindableTexture), textureLoader);
            content.RegisterContentLoader(typeof(TextureData), textureDataLoader);

            _frameConverter = new VideoFrameConverter();
            var mediaFileLoader = new MediaClipLoader(content, _frameConverter, _audioDevice.AudioParameters);
            content.RegisterContentLoader(typeof(MediaPlaybackSession), mediaFileLoader);

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
                DestroyDeviceResources();
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

        private void RecreateGraphicsResources()
        {
            Content.ReloadTextures();
        }

        private void DestroyDeviceResources()
        {
            Content.DestroyTextures();
        }

        public void Exit()
        {
            _shutdownCancellation.Cancel();
        }

        public void Dispose()
        {
            Content.Dispose();
            FontService.Dispose();
            _wicFactory?.Dispose();
            _swapchain.Dispose();
            _graphicsDevice.Dispose();
            _audioDevice.Dispose();
            _frameConverter.Dispose();
        }
    }
}
