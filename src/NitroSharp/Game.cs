using NitroSharp.Content;
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
using NitroSharp.Experimental;

#nullable enable

namespace NitroSharp
{
    internal readonly struct FrameStamp
    {
        public readonly long FrameId;
        public readonly long StopwatchTicks;

        public FrameStamp(long frameId, long stopwatchTicks)
            => (FrameId, StopwatchTicks) = (frameId, stopwatchTicks);
    }

    public partial class Game : IDisposable
    {
        private readonly bool UseWicOnWindows = true;

        private readonly Stopwatch _gameTimer;
        private readonly Configuration _configuration;
        private readonly CancellationTokenSource _shutdownCancellation;

        private readonly GameWindow _window;
        private volatile bool _needsResize;
        private volatile bool _surfaceDestroyed;
        private GraphicsDevice? _graphicsDevice;
        private Swapchain? _swapchain;
        private readonly TaskCompletionSource<int> _initializingGraphics;

        private AudioDevice? _audioDevice;
        private AudioSourcePool? _audioSourcePool;

        private readonly World _world;
        private ContentManager? _content;

        private Presenter? _presenter;
        private ScriptRunner? _scriptRunner;
        private readonly Logger _logger;
        private readonly LogEventRecorder _logEventRecorder;

        private readonly GlyphRasterizer _glyphRasterizer;

        public Game(GameWindow window, Configuration configuration)
        {
            _shutdownCancellation = new CancellationTokenSource();
            _initializingGraphics = new TaskCompletionSource<int>();
            _configuration = configuration;
            (_logger, _logEventRecorder) = SetupLogging();
            _glyphRasterizer = new GlyphRasterizer(enableOutlines: true);
            _window = window;
            _window.Mobile_SurfaceCreated += OnSurfaceCreated;
            _window.Resized += () => _needsResize = true;
            _window.Mobile_SurfaceDestroyed += () => _surfaceDestroyed = true;

            _gameTimer = new Stopwatch();
            _world = new World();
            FontConfiguration = default!;
        }

        internal ContentManager Content => _content!;
        internal AudioDevice AudioDevice => _audioDevice!;
        internal AudioSourcePool AudioSourcePool => _audioSourcePool!;

        internal Logger Logger => _logger;
        internal LogEventRecorder LogEventRecorder => _logEventRecorder;

        internal GlyphRasterizer GlyphRasterizer => _glyphRasterizer;
        internal FontConfiguration FontConfiguration { get; private set; }

        public async Task Run(bool useDedicatedThread = false)
        {
            // Blocking the thread here because desktop platforms
            // require that the main loop be run on the main thread.
            // useDedicatedThread is only used by the Android verison
            // at this point.
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

        private (Logger, LogEventRecorder) SetupLogging()
        {
            var recorder = new LogEventRecorder();
            Logger logger = new LoggerConfiguration()
                .WithSink(recorder)
                .CreateLogger();
            return (logger, recorder);
        }

        private async Task Initialize()
        {
            var initializeAudio = Task.Run(SetupAudio);
            SetupFonts();
            await Task.WhenAll(new[] { _initializingGraphics.Task, initializeAudio });
            _content = CreateContentManager();
            _scriptRunner = new ScriptRunner(this, _world);
            var loadScriptTask = Task.Run(_scriptRunner.LoadStartupScript);
            _presenter = new Presenter(this, _world);
            await loadScriptTask;
        }

        private void SetupFonts()
        {
            _glyphRasterizer.AddFonts(Directory.EnumerateFiles("Fonts"));
            var defaultFont = new FontKey(_configuration.FontFamily, FontStyle.Regular);
            FontConfiguration = new FontConfiguration(
                defaultFont,
                italicFont: null,
                new PtFontSize(_configuration.FontSize),
                defaultTextColor: RgbaFloat.White,
                defaultOutlineColor: RgbaFloat.Black,
                rubyFontSizeMultiplier: 0.4f
            );
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

        private void MainLoop()
        {
            _gameTimer.Start();

            long prevFrameTicks = 0L;
            long frameId = 0L;
            while (!_shutdownCancellation.IsCancellationRequested && _window.Exists)
            {
                if (_surfaceDestroyed)
                {
                    HandleSurfaceDestroyed();
                    return;
                }
                Debug.Assert(_swapchain != null);
                if (_needsResize)
                {
                    Size newSize = _window.Size;
                    _swapchain.Resize(newSize.Width, newSize.Height);
                    _needsResize = false;
                }

                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (float)(currentFrameTicks - prevFrameTicks)
                    / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                try
                {
                    var framestamp = new FrameStamp(frameId++, _gameTimer.ElapsedTicks);
                    Tick(framestamp, deltaMilliseconds);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private void Tick(FrameStamp framestamp, float deltaMilliseconds)
        {
            Debug.Assert(_scriptRunner != null);
            Debug.Assert(_presenter != null);
            if (Content.ResolveTextures())
            {
                _world.BeginFrame();
                _presenter.ProcessChoices();

                var scriptRunnerStatus = _scriptRunner.Tick();
                switch (scriptRunnerStatus)
                {
                    case ScriptRunner.Status.AwaitingPresenterState:
                        _scriptRunner.SyncTo(_presenter);
                        break;
                    case ScriptRunner.Status.NewStateReady:
                        _presenter.SyncTo(_scriptRunner);
                        break;
                    case ScriptRunner.Status.Crashed:
                        throw _scriptRunner.LastException!;
                    case ScriptRunner.Status.Running:
                    default:
                        break;
                }
            }

            _presenter.Tick(framestamp, deltaMilliseconds);
        }

        private void OnSurfaceCreated(SwapchainSource swapchainSource)
        {
            bool resuming = _initializingGraphics.Task.IsCompleted;
            SetupGraphics(swapchainSource);
            Debug.Assert(_graphicsDevice != null);
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
            options.PreferStandardClipSpaceYDirection = true;
#if DEBUG
            options.Debug = true;
#endif
            GraphicsBackend backend = _configuration.PreferredGraphicsBackend
                ?? VeldridStartup.GetPlatformDefaultBackend();
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
                    _graphicsDevice = backend switch
                    {
                        GraphicsBackend.Direct3D11 => GraphicsDevice.CreateD3D11(options),
                        GraphicsBackend.Vulkan => GraphicsDevice.CreateVulkan(options),
                        GraphicsBackend.Metal => GraphicsDevice.CreateMetal(options),
                        _ => ThrowHelper.Unreachable<GraphicsDevice>()
                    };
                }
                _swapchain = _graphicsDevice.ResourceFactory.CreateSwapchain(ref swapchainDesc);
            }
        }

        private void SetupAudio()
        {
            var audioParameters = AudioParameters.Default;
            AudioBackend backend = _configuration.PreferredAudioBackend
                ?? AudioDevice.GetPlatformDefaultBackend();
            _audioDevice = AudioDevice.Create(backend, audioParameters);
            _audioSourcePool = new AudioSourcePool(_audioDevice);
        }

        private ContentManager CreateContentManager()
        {
            Debug.Assert(_graphicsDevice != null);
            Debug.Assert(_audioDevice != null);
            TextureLoader textureLoader;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && UseWicOnWindows)
            {
                textureLoader = new WicTextureLoader(_graphicsDevice);
            }
            else
            {
                textureLoader = new FFmpegTextureLoader(_graphicsDevice);
            }

            var content = new ContentManager(
                _configuration.ContentRoot,
                _graphicsDevice,
                textureLoader,
                _audioDevice
            );
            return content;
        }

        private void HandleSurfaceDestroyed()
        {
            Debug.Assert(_graphicsDevice != null);
            Debug.Assert(_swapchain != null);
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
            _presenter?.Dispose();
            _graphicsDevice?.WaitForIdle();
            _content?.Dispose();
            _glyphRasterizer.Dispose();
            _swapchain?.Dispose();
            _graphicsDevice?.Dispose();
            _audioSourcePool?.Dispose();
            _audioDevice?.Dispose();
        }
    }
}
