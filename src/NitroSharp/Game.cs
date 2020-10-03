using NitroSharp.Media;
using NitroSharp.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;
using System.Runtime.InteropServices;
using System.Text;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;

[assembly: InternalsVisibleTo("NitroSharp.Tests")]

#nullable enable

namespace NitroSharp
{
    internal readonly struct FrameStamp
    {
        public readonly long FrameId;
        public readonly long StopwatchTicks;

        public FrameStamp(long frameId, long stopwatchTicks)
            => (FrameId, StopwatchTicks) = (frameId, stopwatchTicks);

        public static FrameStamp Invalid => new FrameStamp(-1, -1);
        public bool IsValid => FrameId >= 0 && StopwatchTicks >= 0;
    }

    internal enum WaitCondition
    {
        None,
        UserInput,
        MoveCompleted,
        ZoomCompleted,
        RotateCompleted,
        FadeCompleted,
        BezierMoveCompleted,
        TransitionCompleted,
        EntityIdle,
        FrameReady,
        LineRead
    }

    internal readonly struct WaitOperation
    {
        public readonly ThreadContext Thread;
        public readonly WaitCondition Condition;
        public readonly EntityQuery? EntityQuery;
        public readonly Texture? ScreenshotTexture;

        public WaitOperation(
            ThreadContext thread,
            WaitCondition condition,
            EntityQuery? entityQuery,
            Texture? screenshotTexture = null)
        {
            Thread = thread;
            Condition = condition;
            EntityQuery = entityQuery;
            ScreenshotTexture = screenshotTexture;
        }

        public void Deconstruct(out WaitCondition condition, out EntityQuery? query)
        {
            condition = Condition;
            query = EntityQuery;
        }
    }

    internal sealed class GameContext
    {
        public GameWindow Window { get; }
        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public FontConfiguration FontConfig { get; }
        public RenderContext RenderContext { get; }
        public InputContext InputContext { get; }
        public NsScriptVM VM { get; }
        public World World { get; }

        public Queue<WaitOperation> WaitOperations { get; }
        public CancellationTokenSource ShutdownSignal { get; }

        public GameContext(
            GameWindow window,
            ContentManager content,
            GlyphRasterizer glyphRasterizer,
            FontConfiguration fontConfig,
            RenderContext renderContext,
            InputContext inputContext,
            NsScriptVM vm,
            World world)
        {
            Window = window;
            Content = content;
            GlyphRasterizer = glyphRasterizer;
            FontConfig = fontConfig;
            RenderContext = renderContext;
            InputContext = inputContext;
            VM = vm;
            World = world;
            WaitOperations = new Queue<WaitOperation>();
            ShutdownSignal = new CancellationTokenSource();
        }

        public void Wait(
            ThreadContext thread,
            WaitCondition condition,
            TimeSpan? timeout = null,
            EntityQuery? entityQuery = null,
            Texture? screenshotTexture = null)
        {
            VM.SuspendThread(thread, timeout);
            if (condition != WaitCondition.None)
            {
                WaitOperations.Enqueue(
                    new WaitOperation(thread, condition, entityQuery, screenshotTexture)
                );
            }
        }
    }

    public class Game : IDisposable
    {
        private readonly bool UseWicOnWindows = true;

        private readonly Stopwatch _gameTimer;
        private readonly Configuration _configuration;
        private readonly Logger _logger;
        private readonly LogEventRecorder _logEventRecorder;

        private readonly GameWindow _window;
        private volatile bool _needsResize;
        private volatile bool _surfaceDestroyed;
        private GraphicsDevice? _graphicsDevice;
        private Swapchain? _swapchain;
        private readonly TaskCompletionSource<int> _initializingGraphics;
        private RenderSystem? _renderSystem;
        private readonly GlyphRasterizer _glyphRasterizer;
        private FontConfiguration? _fontConfig;

        private ContentManager? _content;
        private readonly InputContext _inputContext;

        private AudioDevice? _audioDevice;
        private AudioSourcePool? _audioSourcePool;

        private readonly string _nssFolder;
        private readonly string _bytecodeCacheDir;
        private NsScriptVM? _vm;
        private Builtins _builtinFunctions;
        private readonly World _world;
        private GameContext? _context;

        private readonly List<WaitOperation> _survivedWaits;

        public Game(GameWindow window, Configuration configuration)
        {
            _initializingGraphics = new TaskCompletionSource<int>();
            _configuration = configuration;
            (_logger, _logEventRecorder) = SetupLogging();
            _glyphRasterizer = new GlyphRasterizer();
            _window = window;
            _window.Mobile_SurfaceCreated += OnSurfaceCreated;
            _window.Resized += () => _needsResize = true;
            _window.Mobile_SurfaceDestroyed += () => _surfaceDestroyed = true;

            _gameTimer = new Stopwatch();
            _inputContext = new InputContext(window);

            _nssFolder = Path.Combine(_configuration.ContentRoot, "nss");
            _bytecodeCacheDir = _nssFolder.Replace("nss", "nsx");
            _builtinFunctions = null!;
            _survivedWaits = new List<WaitOperation>();
            _world = new World();
        }

        internal AudioDevice AudioDevice => _audioDevice!;
        internal AudioSourcePool AudioSourcePool => _audioSourcePool!;

        internal Logger Logger => _logger;
        internal LogEventRecorder LogEventRecorder => _logEventRecorder;

        public Task Run(bool useDedicatedThread = false)
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

            _context = new GameContext(
                _window,
                _content!,
                _glyphRasterizer,
                _fontConfig!,
                _renderSystem!.Context,
                _inputContext,
                _vm!,
                _world
            );
            _builtinFunctions = new Builtins(_context);
            return RunMainLoop(useDedicatedThread);
        }

        private async Task Initialize()
        {
            var initializeAudio = Task.Run(SetupAudio);
            SetupFonts();
            await Task.WhenAll(_initializingGraphics.Task, initializeAudio);
            _content = CreateContentManager();
            await Task.Run(LoadStartupScript);
            _renderSystem = new RenderSystem(
                _configuration,
                _graphicsDevice!,
                _swapchain!,
                _glyphRasterizer,
                _content,
                _vm!.SystemVariables
            );
        }

        private static (Logger, LogEventRecorder) SetupLogging()
        {
            var recorder = new LogEventRecorder();
            Logger logger = new LoggerConfiguration()
                .WithSink(recorder)
                .CreateLogger();
            return (logger, recorder);
        }

        private void SetupFonts()
        {
            _glyphRasterizer.AddFonts(Directory.EnumerateFiles("Fonts"));
            var defaultFont = new FontFaceKey(_configuration.FontFamily, FontStyle.Regular);
            _fontConfig = new FontConfiguration(
                defaultFont,
                italicFont: null,
                new PtFontSize(_configuration.FontSize),
                defaultTextColor: RgbaFloat.White,
                defaultOutlineColor: RgbaFloat.Black,
                rubyFontSizeMultiplier: 0.4f
            );
        }

        private void LoadStartupScript()
        {
            const string globalsFileName = "_globals";
            string globalsPath = Path.Combine(_bytecodeCacheDir, globalsFileName);
            if (_configuration.SkipUpToDateCheck || !File.Exists(globalsPath) || !ValidateBytecodeCache())
            {
                if (!Directory.Exists(_bytecodeCacheDir))
                {
                    Directory.CreateDirectory(_bytecodeCacheDir);
                    _logger.LogInformation("Bytecode cache is empty. Compiling the scripts...");
                }
                else
                {
                    _logger.LogInformation("Bytecode cache is not up-to-date. Recompiling the scripts...");
                    foreach (string file in Directory
                        .EnumerateFiles(_bytecodeCacheDir, "*.nsx", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }

                Encoding? sourceEncoding = _configuration.UseUtf8 ? Encoding.UTF8 : null;
                var compilation = new Compilation(
                    _nssFolder,
                    _bytecodeCacheDir,
                    globalsFileName,
                    sourceEncoding
                );
                compilation.Emit(compilation.GetSourceModule(_configuration.SysScripts.Startup));
            }
            else
            {
                _logger.LogInformation("Bytecode cache is up-to-date.");
            }

            var nsxLocator = new FileSystemNsxModuleLocator(_bytecodeCacheDir);
            _vm = new NsScriptVM(nsxLocator, File.OpenRead(globalsPath));
            _vm.CreateThread(
                Path.ChangeExtension(_configuration.SysScripts.Startup, null),
                symbol: "main"
            );
        }

        private bool ValidateBytecodeCache()
        {
            static string getModulePath(string rootDir, string fullPath)
            {
                return Path.ChangeExtension(
                    Path.GetRelativePath(rootDir, fullPath),
                    extension: null
                );
            }

            string startupModule = getModulePath(
                rootDir: _nssFolder,
                Path.Combine(_nssFolder, _configuration.SysScripts.Startup)
            );
            foreach (string nssFile in Directory
                .EnumerateFiles(_nssFolder, "*.nss", SearchOption.AllDirectories))
            {
                string currentModule = getModulePath(rootDir: _nssFolder, nssFile);
                string nsxPath = Path.ChangeExtension(
                    Path.Combine(_bytecodeCacheDir, currentModule),
                    "nsx"
                );
                try
                {
                    using (FileStream nsxStream = File.OpenRead(nsxPath))
                    {
                        long nsxTimestamp = NsxModule.GetSourceModificationTime(nsxStream);
                        long nssTimestamp = new DateTimeOffset(File.GetLastWriteTimeUtc(nssFile))
                            .ToUnixTimeSeconds();
                        if (nsxTimestamp != nssTimestamp)
                        {
                            return false;
                        }
                    }

                }
                catch
                {
                    if (currentModule.Equals(startupModule, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private Task RunMainLoop(bool useDedicatedThread = false)
        {
            if (useDedicatedThread)
            {
                return Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
            }
            try
            {
                MainLoop();
            }
            catch (TaskCanceledException)
            {
            }
            return Task.FromResult(0);
        }

        private void MainLoop()
        {
            _gameTimer.Start();

            long prevFrameTicks = 0L;
            long frameId = 0L;
            Debug.Assert(_context is object);
            while (!_context.ShutdownSignal.IsCancellationRequested && _window.Exists)
            {
                if (_surfaceDestroyed)
                {
                    HandleSurfaceDestroyed();
                    return;
                }
                Debug.Assert(_swapchain is object);
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

        private void Tick(FrameStamp framestamp, float dt)
        {
            Debug.Assert(_renderSystem is object);
            Debug.Assert(_content is object);
            Debug.Assert(_context is object);
            Debug.Assert(_vm is object);

            _renderSystem.BeginFrame(framestamp);
            RunResult runResult = _vm.Run(_builtinFunctions, _context.ShutdownSignal.Token);
            foreach (uint thread in runResult.TerminatedThreads)
            {
                _world.DestroyContext(thread);
            }

            bool assetsReady = _content.ResolveAssets();
            if (assetsReady)
            {
                _world.BeginFrame();
            }
            _renderSystem.Render(_context, _world.RenderItems, dt, assetsReady);
            _renderSystem.EndFrame();
            if (assetsReady)
            {
                // That's right, the input is processed after the frame's been rendered,
                // and only if all of the requested assets have been loaded.
                // These two conditions have to be met in order to perform
                // pixel-perfect hit testing.
                _inputContext.Update(_vm);
                _renderSystem.ProcessChoices(_world, _inputContext, _window);
                ProcessWaitOperations();
            }
            try
            {
                _renderSystem.Present();
            }
            catch (VeldridException e)
                when (e.Message == "The Swapchain's underlying surface has been lost.")
            {
            }
        }

        private void ProcessWaitOperations()
        {
            Debug.Assert(_context is object);
            Debug.Assert(_vm is object);
            Debug.Assert(_renderSystem is object);

            Queue<WaitOperation> waits = _context.WaitOperations;
            while (waits.TryDequeue(out WaitOperation wait))
            {
                if (wait.Thread.IsActive) { continue; }
                if (ShouldResume(wait))
                {
                    _vm.ResumeThread(wait.Thread);
                    if (wait.ScreenshotTexture is Texture screenshotTexture)
                    {
                        _renderSystem.Context.CaptureFramebuffer(screenshotTexture);
                    }
                }
                else
                {
                    _survivedWaits.Add(wait);
                }
            }

            foreach (WaitOperation wait in _survivedWaits)
            {
                waits.Enqueue(wait);
            }

            _survivedWaits.Clear();
        }

        private bool ShouldResume(in WaitOperation wait)
        {
            uint contextId = wait.Thread.Id;

            bool checkInput() => _inputContext.VKeyDown(VirtualKey.Advance);

            bool checkIdle(EntityQuery query)
            {
                foreach (Entity entity in _world.Query(contextId, query))
                {
                    if (!entity.IsIdle) { return false; }
                }

                return true;
            }

            bool checkAnim(EntityQuery query, AnimationKind anim)
            {
                foreach (RenderItem entity in _world.Query<RenderItem>(contextId, query))
                {
                    if (entity.IsAnimationActive(anim)) { return false; }
                }

                return true;
            }

            bool checkLineRead(EntityQuery query)
            {
                foreach (DialoguePage page in _world.Query<DialoguePage>(contextId, query))
                {
                    if (page.LineRead) { return true; }
                }

                return false;
            }

            return wait switch
            {
                (WaitCondition.UserInput, _) => checkInput(),
                (WaitCondition.EntityIdle, { } query) => checkIdle(query),
                (WaitCondition.FadeCompleted, { } query) => checkAnim(query, AnimationKind.Fade),
                (WaitCondition.MoveCompleted, { } query) => checkAnim(query, AnimationKind.Move),
                (WaitCondition.ZoomCompleted, { } query) => checkAnim(query, AnimationKind.Zoom),
                (WaitCondition.RotateCompleted, { } query) => checkAnim(query, AnimationKind.Rotate),
                (WaitCondition.BezierMoveCompleted, { } query) => checkAnim(query, AnimationKind.BezierMove),
                (WaitCondition.TransitionCompleted, { } query) => checkAnim(query, AnimationKind.Transition),
                (WaitCondition.FrameReady, _) => true,
                (WaitCondition.LineRead, { } query) => checkLineRead(query),
                _ => false
            };
        }

        private void OnSurfaceCreated(SwapchainSource swapchainSource)
        {
            bool resuming = _initializingGraphics.Task.IsCompleted;
            SetupGraphics(swapchainSource);
            Debug.Assert(_graphicsDevice is object);
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

           // if (backend == GraphicsBackend.OpenGLES || backend == GraphicsBackend.OpenGL)
           // {
           //     _graphicsDevice = _window is DesktopWindow desktopWindow
           //         ? VeldridStartup.CreateDefaultOpenGLGraphicsDevice(options, desktopWindow.SdlWindow, backend)
           //         : GraphicsDevice.CreateOpenGLES(options, swapchainDesc);
           //     _swapchain = _graphicsDevice.MainSwapchain;
           // }
           // else
            {
                if (_graphicsDevice == null)
                {
                    _graphicsDevice = backend switch
                    {
                        GraphicsBackend.Direct3D11 => GraphicsDevice.CreateD3D11(options),
                        GraphicsBackend.Vulkan => GraphicsDevice.CreateVulkan(options),
                        //GraphicsBackend.Metal => GraphicsDevice.CreateMetal(options),
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
            Debug.Assert(_graphicsDevice is object);
            Debug.Assert(_audioDevice is object);
            TextureLoader textureLoader;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && UseWicOnWindows)
            {
                textureLoader = new WicTextureLoader(_graphicsDevice);
            }
            else
            {
                textureLoader = null!;
                //textureLoader = new FFmpegTextureLoader(_graphicsDevice);
            }

            var content = new ContentManager(_configuration.ContentRoot, textureLoader);
            return content;
        }

        private void HandleSurfaceDestroyed()
        {
            Debug.Assert(_graphicsDevice is object);
            Debug.Assert(_swapchain is object);
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

        public void Dispose()
        {
            _inputContext.Dispose();
            _graphicsDevice?.WaitForIdle();
            _world.Dispose();
            _renderSystem?.Dispose();
            _content?.Dispose();
            _glyphRasterizer.Dispose();
            _swapchain?.Dispose();
            _graphicsDevice?.Dispose();
            _window.Dispose();
            _audioSourcePool?.Dispose();
            _audioDevice?.Dispose();
        }
    }
}
