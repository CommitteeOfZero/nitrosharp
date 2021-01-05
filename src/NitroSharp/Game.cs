using NitroSharp.Media;
using NitroSharp.Text;
using System;
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
using System.Collections.Generic;
using System.Numerics;
using NitroSharp.Saving;

[assembly: InternalsVisibleTo("NitroSharp.Tests")]
[assembly: InternalsVisibleTo("Game")]

#nullable enable

namespace NitroSharp
{
    internal readonly struct FrameStamp
    {
        public readonly long FrameId;
        public readonly long StopwatchTicks;

        public FrameStamp(long frameId, long stopwatchTicks)
            => (FrameId, StopwatchTicks) = (frameId, stopwatchTicks);

        public static FrameStamp Invalid => new(-1, -1);
        public bool IsValid => FrameId >= 0 && StopwatchTicks >= 0;
    }

    internal enum DeferredOperationKind
    {
        CaptureFramebuffer,
        SaveGame,
        LoadGame
    }

    internal readonly struct DeferredOperation
    {
        public DeferredOperationKind Kind { get; init; }
        public Texture? ScreenshotTexture { get; init; }
        public uint? SaveSlot { get; init; }

        public static DeferredOperation CaptureFramebuffer(Texture dstTexture) => new()
        {
            Kind = DeferredOperationKind.CaptureFramebuffer,
            ScreenshotTexture = dstTexture,
            SaveSlot = null
        };

        public static DeferredOperation SaveGame(uint slot) => new()
        {
            Kind = DeferredOperationKind.SaveGame,
            ScreenshotTexture = null,
            SaveSlot = slot
        };

        public static DeferredOperation LoadGame(uint slot) => new()
        {
            Kind = DeferredOperationKind.LoadGame,
            ScreenshotTexture = null,
            SaveSlot = slot
        };
    }

    internal sealed class GameContext
    {
        public Stopwatch Timer { get; }

        public GameWindow Window { get; }
        public Configuration Config { get; }
        public GameSaveManager SaveManager { get; }
        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public RenderContext RenderContext { get; }
        public InputContext InputContext { get; }
        public NsScriptVM VM { get; }
        public CancellationTokenSource ShutdownSignal { get; }

        public GameProcess MainProcess { get; set; }
        public GameProcess? SysProcess { get; set; }
        public GameProcess ActiveProcess => SysProcess ?? MainProcess;

        public Backlog Backlog { get; }

        public Texture? LastScreenshot { get; set; }

        public Queue<DeferredOperation> DeferredOperations { get; } = new();

        public GameContext(
            GameWindow window,
            Configuration config,
            GameSaveManager saveManager,
            ContentManager content,
            GlyphRasterizer glyphRasterizer,
            RenderContext renderContext,
            InputContext inputContext,
            NsScriptVM vm,
            GameProcess mainProcess)
        {
            Window = window;
            Config = config;
            SaveManager = saveManager;
            Content = content;
            GlyphRasterizer = glyphRasterizer;
            RenderContext = renderContext;
            InputContext = inputContext;
            VM = vm;
            ShutdownSignal = new CancellationTokenSource();
            MainProcess = mainProcess;
            Backlog = new Backlog(vm.SystemVariables);
            Timer = Stopwatch.StartNew();
        }

        public void Defer(in DeferredOperation operation)
        {
            ActiveProcess.VmProcess.Suspend();
            DeferredOperations.Enqueue(operation);
        }

        public void Wait(
            NsScriptThread thread,
            WaitCondition condition,
            TimeSpan? timeout = null,
            EntityQuery? entityQuery = null)
        {
            ActiveProcess.Wait(thread, condition, timeout, entityQuery);
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
        private readonly FontConfiguration _defaultFontConfig;
        private readonly GameSaveManager _saveManager;

        private ContentManager? _content;
        private readonly InputContext _inputContext;

        private AudioDevice? _audioDevice;
        private AudioSourcePool? _audioSourcePool;

        private readonly string _nssFolder;
        private readonly string _bytecodeCacheDir;
        private NsScriptVM? _vm;
        private Builtins _builtinFunctions = null!;
        private GameContext? _context;

        private bool _clearFramebuffer = true;

        public Game(GameWindow window, Configuration configuration)
        {
            _initializingGraphics = new TaskCompletionSource<int>();
            _configuration = configuration;
            _saveManager = new GameSaveManager(configuration);
            (_logger, _logEventRecorder) = SetupLogging();
            _glyphRasterizer = new GlyphRasterizer();
            _window = window;
            _window.Mobile_SurfaceCreated += OnSurfaceCreated;
            _window.Resized += () => _needsResize = true;
            _window.Mobile_SurfaceDestroyed += () => _surfaceDestroyed = true;

            _gameTimer = new Stopwatch();
            _inputContext = new InputContext(window);

            _nssFolder = Path.Combine(_configuration.ContentRoot, _configuration.ScriptRoot);
            _bytecodeCacheDir = _nssFolder.Replace("nss", "nsx");

            var defaultFont = new FontFaceKey(_configuration.FontFamily, FontStyle.Regular);
            _defaultFontConfig = new FontConfiguration(
                defaultFont,
                italicFont: null,
                new PtFontSize(_configuration.FontSize),
                defaultTextColor: Vector4.One,
                defaultOutlineColor: Vector4.Zero,
                rubyFontSizeMultiplier: 0.4f
            );
        }

        private void SetSystemVariables()
        {
            Debug.Assert(_context is not null);
            _saveManager.ReadCommonSaveData(_context);
            SystemVariableLookup vars = _context.VM.SystemVariables;
            vars.SavePath = ConstantValue.String(_saveManager.SaveDirectory);
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
            GameProcess process;
            try
            {
                process = Initialize().Result;
            }
            catch (AggregateException aex)
            {
                throw aex.Flatten();
            }

            _context = new GameContext(
                _window,
                _configuration,
                _saveManager,
                _content!,
                _glyphRasterizer,
                _renderSystem!.Context,
                _inputContext,
                _vm!,
                process
            );
            _builtinFunctions = new Builtins(_context);


            SetSystemVariables();

            return RunMainLoop(useDedicatedThread);
        }

        private async Task<GameProcess> Initialize()
        {
            //var initializeAudio = Task.Run(SetupAudio);
            _glyphRasterizer.AddFonts(Directory.EnumerateFiles("Fonts"));
            await Task.WhenAll(_initializingGraphics.Task);
            _content = CreateContentManager();
            GameProcess mainProcess = await Task.Run(LoadStartupScript);
            _renderSystem = new RenderSystem(
                _configuration,
                _graphicsDevice!,
                _swapchain!,
                _glyphRasterizer,
                _content,
                _vm!.SystemVariables
            );
            return mainProcess;
        }

        private static (Logger, LogEventRecorder) SetupLogging()
        {
            var recorder = new LogEventRecorder();
            Logger logger = new LoggerConfiguration()
                .WithSink(recorder)
                .CreateLogger();
            return (logger, recorder);
        }

        private GameProcess LoadStartupScript()
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

                SourceModuleSymbol startup = compilation.GetSourceModule(_configuration.SysScripts.Startup);
                SourceModuleSymbol backlog = compilation.GetSourceModule(_configuration.SysScripts.Backlog);
                SourceModuleSymbol save = compilation.GetSourceModule(_configuration.SysScripts.Save);
                SourceModuleSymbol load = compilation.GetSourceModule(_configuration.SysScripts.Load);
                SourceModuleSymbol menu = compilation.GetSourceModule(_configuration.SysScripts.Menu);

                compilation.Emit(new[] { startup, backlog, save, load, menu });
            }
            else
            {
                _logger.LogInformation("Bytecode cache is up-to-date.");
            }

            var nsxLocator = new FileSystemNsxModuleLocator(_bytecodeCacheDir);
            _vm = new NsScriptVM(nsxLocator, File.OpenRead(globalsPath));
            return CreateProcess(_configuration.SysScripts.Startup);
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

            _renderSystem.BeginFrame(framestamp, _clearFramebuffer);
            _clearFramebuffer = true;

            while (true)
            {
                GameProcess activeProcess = _context.ActiveProcess;
                RunResult runResult = _vm.Run(
                    activeProcess.VmProcess,
                    _builtinFunctions,
                    _context.ShutdownSignal.Token
                );

                foreach (uint thread in runResult.TerminatedThreads)
                {
                    activeProcess.World.DestroyContext(thread);
                }

                if (_context.SysProcess is GameProcess { VmProcess: { IsTerminated: true } } sysProc)
                {
                    sysProc.Dispose();
                    _context.SysProcess = null;
                    _context.MainProcess.VmProcess.Resume();
                    continue;
                }

                break;
            }


            World world = _context.ActiveProcess.World;
            bool assetsReady = _content.ResolveAssets();
            if (assetsReady)
            {
                world.BeginFrame();
            }
            _renderSystem.Render(framestamp, _context, world.RenderItems, dt, assetsReady);
            RunDeferredOperations();
            _renderSystem.EndFrame();

            if (assetsReady)
            {
                // That's right, the input is processed after the frame's been rendered,
                // and only if all of the requested assets have been loaded.
                // These two conditions have to be met in order to perform
                // pixel-perfect hit testing.
                _inputContext.Update(_vm.SystemVariables);
                _renderSystem.ProcessChoices(world, _inputContext, _window);
                _context.ActiveProcess.ProcessWaitOperations(_context);

                if (_inputContext.WheelDelta > 0)
                {
                    if (CreateSysProcess(_configuration.SysScripts.Backlog))
                    {
                        _clearFramebuffer = false;
                    }
                }
                if (_inputContext.RawInput.IsMouseDown(MouseButton.Right))
                {
                    if (CreateSysProcess(_configuration.SysScripts.Menu))
                    {
                        _clearFramebuffer = false;
                    }
                }
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

        private void RunDeferredOperations()
        {
            Debug.Assert(_context is not null);
            Debug.Assert(_renderSystem is not null);
            bool resumeProcess = _context.DeferredOperations.Count > 0;
            while (_context.DeferredOperations.TryDequeue(out DeferredOperation op))
            {
                switch (op.Kind)
                {
                    case DeferredOperationKind.CaptureFramebuffer:
                        Debug.Assert(op.ScreenshotTexture is not null);
                        _renderSystem.Context.CaptureFramebuffer(op.ScreenshotTexture);
                        break;
                    case DeferredOperationKind.SaveGame:
                        Debug.Assert(op.SaveSlot is not null);
                        _saveManager.Save(_context, op.SaveSlot.Value);
                        break;
                    case DeferredOperationKind.LoadGame:
                        Debug.Assert(op.SaveSlot is not null);
                        _saveManager.Load(_context, op.SaveSlot.Value);
                        break;
                }
            }

            if (resumeProcess)
            {
                _context.ActiveProcess.VmProcess.Resume();
            }
        }

        private bool CreateSysProcess(string mainModule)
        {
            Debug.Assert(_context is not null);
            //_context.SysProcess?.Dispose();
            if (_context.SysProcess is null)
            {
                _context.LastScreenshot ??= _context.RenderContext.CreateFullscreenTexture(staging: true);
                _context.RenderContext.CaptureFramebuffer(_context.LastScreenshot);
                _context.MainProcess.VmProcess.Suspend();
                _context.SysProcess = CreateProcess(mainModule);
                return true;
            }

            return false;
        }

        private GameProcess CreateProcess(string mainModule)
        {
            mainModule = Path.ChangeExtension(mainModule, null);
            return new GameProcess(_vm!.CreateProcess(mainModule, "main"), _defaultFontConfig.Clone());
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
               _graphicsDevice ??= backend switch
               {
                   GraphicsBackend.Direct3D11 => GraphicsDevice.CreateD3D11(options),
                   GraphicsBackend.Vulkan => GraphicsDevice.CreateVulkan(options),
                   //GraphicsBackend.Metal => GraphicsDevice.CreateMetal(options),
                   _ => ThrowHelper.Unreachable<GraphicsDevice>()
               };
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
            //Debug.Assert(_audioDevice is object);
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
            _context!.MainProcess.Dispose();
            _context!.SysProcess?.Dispose();
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
