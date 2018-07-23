using NitroSharp.Animation;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Execution;
using NitroSharp.Primitives;
using NitroSharp.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;

namespace NitroSharp
{
    public sealed class Game : IDisposable
    {
        private readonly Stopwatch _gameTimer;
        private readonly Configuration _configuration;
        private readonly CancellationTokenSource _shutdownCancellation;

        private GameWindow _window;
        private volatile bool _needsResize;
        private volatile bool _surfaceDestroyed;
        private GraphicsDevice _graphicsDevice;
        private Swapchain _swapchain;
        private TaskCompletionSource<int> _initializingGraphics;

        private readonly SystemManager _systems;
        private RenderSystem _renderSystem;
        private InputSystem _inputHandler;

        private readonly string _nssFolder;
        private NsScriptInterpreter _nssInterpreter;
        private readonly CoreLogic _coreLogic;
        private Task _interpreterProc;
        private volatile bool _nextStateReady;

        private VideoFrameConverter _frameConverter;
        private SharpDX.WIC.ImagingFactory _wicFactory;
        private AudioDevice _audioDevice;
        private AudioSourcePool _audioSourcePool;

        public Game(GameWindow window, Configuration configuration)
        {
            _shutdownCancellation = new CancellationTokenSource();
            _initializingGraphics = new TaskCompletionSource<int>();

            _configuration = configuration;
            _nssFolder = Path.Combine(configuration.ContentRoot, "nss");
            _window = window;
            _window.Mobile_SurfaceCreated += OnSurfaceCreated;
            _window.Resized += OnWindowResized;
            _window.Mobile_SurfaceDestroyed += OnSurfaceDestroyed;

            _gameTimer = new Stopwatch();
            var entities = new EntityManager(_gameTimer);
            _systems = new SystemManager(entities);
            _coreLogic = new CoreLogic(this, entities);
        }

        internal ContentManager Content { get; private set; }
        internal FontService FontService { get; private set; }

        internal AudioDevice AudioDevice => _audioDevice;
        internal AudioSourcePool AudioSourcePool => _audioSourcePool;

        public async Task Run(bool useDedicatedThread = false)
        {
            var loadScriptTask = Task.Run((Action)LoadStartupScript);
            var initializeAudio = Task.Run((Action)SetupAudio);

            await Task.WhenAll(_initializingGraphics.Task, initializeAudio);

            CreateServices();
            RegisterSystems();
            _coreLogic.InitializeResources();
            await loadScriptTask;
            StartInterpreter();

            await RunMainLoop(useDedicatedThread);
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

        private void CreateServices()
        {
            Content = CreateContentManager();
            Content.SetGraphicsDevice(_graphicsDevice);
            FontService = CreateFontService();
        }

        private ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);
            ContentLoader textureLoader = null;
            ContentLoader textureDataLoader = null;

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    _wicFactory = new SharpDX.WIC.ImagingFactory();
            //    textureLoader = new WicTextureLoader(content, _wicFactory);
            //    textureDataLoader = new WicTextureDataLoader(content, _wicFactory);
            //}
            //else
            {
                textureLoader = new FFmpegTextureLoader(content);
                textureDataLoader = new FFmpegTextureDataLoader(content);
            }

            content.RegisterContentLoader(typeof(BindableTexture), textureLoader);
            content.RegisterContentLoader(typeof(TextureData), textureDataLoader);

            _frameConverter = new VideoFrameConverter();
            var mediaFileLoader = new MediaFileLoader(content, _frameConverter, _audioDevice.AudioParameters);
            content.RegisterContentLoader(typeof(MediaPlaybackSession), mediaFileLoader);

            return content;
        }

        private void SetupAudio()
        {
            var audioParameters = AudioParameters.Default;
            var backend = _configuration.PreferredAudioBackend ?? AudioDevice.GetPlatformDefaultBackend();
            _audioDevice = AudioDevice.Create(backend, audioParameters);
            _audioSourcePool = new AudioSourcePool(_audioDevice);
        }

        private FontService CreateFontService()
        {
            var fontService = new FontService();
            fontService.RegisterFonts(Directory.EnumerateFiles("Fonts"));
            return fontService;
        }

        private void RegisterSystems()
        {
            _inputHandler = new InputSystem(_window, _coreLogic);
            _systems.Add(_inputHandler);

            var animationSystem = new AnimationSystem();
            _systems.Add(animationSystem);

            _renderSystem = new RenderSystem(_configuration, FontService);
            _renderSystem.CreateDeviceResources(_graphicsDevice, _swapchain);
            _systems.Add(_renderSystem);
        }

        private Task RunMainLoop(bool useDedicatedThread = false)
        {
            if (useDedicatedThread)
            {
                return Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
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

        private void LoadStartupScript()
        {
            _nssInterpreter = new NsScriptInterpreter(LocateScript, _coreLogic);
            _nssInterpreter.CreateThread("__MAIN", _configuration.StartupScript, "main");
        }

        private Stream LocateScript(SourceFileReference fileRef)
        {
            return File.OpenRead(Path.Combine(_nssFolder, fileRef.FilePath.Replace("nss/", string.Empty)));
        }

        private void StartInterpreter()
        {
            _interpreterProc = Task.Factory.StartNew(InterpreterLoop, TaskCreationOptions.LongRunning);
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

                Update(deltaMilliseconds);
            }
        }

        private void InterpreterLoop()
        {
            while (!_shutdownCancellation.IsCancellationRequested)
            {
                while (_nextStateReady)
                {
                    Thread.Sleep(5);
                }

                _nssInterpreter.Run(CancellationToken.None);
                _nextStateReady = true;
            }
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

        private void Update(float deltaMilliseconds)
        {
            try
            {
                UpdateCore(deltaMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void UpdateCore(float deltaMilliseconds)
        {
            if (_nextStateReady)
            {
                _systems.ProcessEntityUpdates();
                _inputHandler.Update(deltaMilliseconds);

                if (!_nssInterpreter.Threads.Any())
                {
                    Exit();
                }

                _nextStateReady = false;
            }
            else if (_interpreterProc.IsFaulted)
            {
                throw _interpreterProc.Exception.InnerException;
            }

            _systems.Update(deltaMilliseconds);
            if (_window.Exists)
            {
                _renderSystem.Present();
            }
        }

        private void RecreateGraphicsResources()
        {
            Content.ReloadTextures();
            _renderSystem.CreateDeviceResources(_graphicsDevice, _swapchain);
        }

        private void DestroyDeviceResources()
        {
            _renderSystem.DestroyDeviceResources();
            Content.DestroyTextures();
        }

        public void Exit()
        {
            _shutdownCancellation.Cancel();
        }

        public void Dispose()
        {
            _systems.Dispose();
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
