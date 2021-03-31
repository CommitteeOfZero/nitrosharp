using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using NitroSharp.Saving;
using NitroSharp.Text;
using Veldrid;
using Veldrid.StartupUtilities;

[assembly: InternalsVisibleTo("NitroSharp.Tests")]
[assembly: InternalsVisibleTo("Game")]

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
        public DeferredOperationKind Kind { get; private init; }
        public Texture? ScreenshotTexture { get; private init; }
        public uint? SaveSlot { get; private init; }

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

    public sealed class GameContext : IAsyncDisposable
    {
        private readonly Logger _logger;
        private readonly LogEventRecorder _logEventRecorder;
        private readonly BuiltInFunctions _builtInFunctions;
        private readonly FontConfiguration _fontConfig;
        private readonly Dictionary<string, MediaStream> _voices = new();
        private (string, MediaStream?) _activeVoice;
        private readonly Queue<DeferredOperation> _deferredOperations = new();
        private bool _clearFramebuffer;

        private GameContext(
            Logger logger,
            LogEventRecorder logEventRecorder,
            GameWindow window,
            Configuration config,
            RenderContext renderContext,
            ContentManager content,
            GlyphRasterizer glyphRasterizer,
            FontConfiguration fontConfig,
            AudioContext audioContext,
            InputContext inputContext,
            NsScriptVM vm,
            GameProcess mainProcess,
            GameSaveManager saveManager)
        {
            Window = window;
            Config = config;
            RenderContext = renderContext;
            Content = content;
            GlyphRasterizer = glyphRasterizer;
            _logger = logger;
            _logEventRecorder = logEventRecorder;
            _fontConfig = fontConfig;
            AudioContext = audioContext;
            InputContext = inputContext;
            VM = vm;
            MainProcess = mainProcess;
            SaveManager = saveManager;
            Backlog = new Backlog(VM.SystemVariables);
            Clock = Stopwatch.StartNew();
            _builtInFunctions = new Builtins(this);
        }

        internal GameWindow Window { get; }
        internal Configuration Config { get; }
        internal RenderContext RenderContext { get; }
        internal ContentManager Content { get; }
        internal GlyphRasterizer GlyphRasterizer { get; }
        internal AudioContext AudioContext { get; }
        internal InputContext InputContext { get;}
        internal NsScriptVM VM { get; }
        internal GameSaveManager SaveManager { get; }
        internal Backlog Backlog { get; }
        internal Stopwatch Clock { get; }
        internal CancellationTokenSource ShutdownSignal { get; } = new();
        internal GameProcess MainProcess { get; set; }
        internal GameProcess? SysProcess { get; set; }
        internal GameProcess ActiveProcess => SysProcess ?? MainProcess;

        internal bool Skipping { get; private set; }
        internal bool Advance { get; set; }
        internal Texture? LastScreenshot { get; private set; }
        internal EntityId FocusedUiElement { get; set; }
        internal NsFocusDirection? RequestedFocusChange { get; set; }

        public static async Task<GameContext> Create(GameWindow window, Configuration configuration)
        {
            var createSurface = new TaskCompletionSource<SwapchainSource>();
            window.Mobile_SurfaceCreated += surf => createSurface.SetResult(surf);
            (Logger logger, LogEventRecorder logEventRecorder) = SetupLogging();
            Task<AudioContext> initAudio = Task.Run(() => InitAudio(configuration));
            Task<(GlyphRasterizer, FontConfiguration)> loadFonts = Task
                .Run(async () => await LoadFonts(configuration));

            (GlyphRasterizer glyphRasterizer, FontConfiguration fontConfig) = await loadFonts;
            Task<(NsScriptVM vm, GameProcess mainProcess)> startVM = Task
                .Run(() => LoadStartupScript(configuration, fontConfig, logger));

            SwapchainSource swapchainSource = await createSurface.Task;
            (GraphicsDevice gd, Swapchain swapchain) = InitGraphics(window, configuration);
            ContentManager contentMgr = CreateContentManager(gd, configuration);
            AudioContext audioContext = await initAudio;
            (NsScriptVM vm, GameProcess mainProcess) = await startVM;

            var inputContext = new InputContext(window);
            var saveManager = new GameSaveManager(configuration);
            var renderContext = new RenderContext(
                window,
                configuration,
                gd,
                swapchain,
                contentMgr,
                glyphRasterizer,
                vm.SystemVariables
            );

            return new GameContext(
                logger,
                logEventRecorder,
                window,
                configuration,
                renderContext,
                contentMgr,
                glyphRasterizer,
                fontConfig,
                audioContext,
                inputContext,
                vm, mainProcess,
                saveManager
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

        private static async Task<(GlyphRasterizer, FontConfiguration)> LoadFonts(
            Configuration configuration)
        {
            var glyphRasterizer = new GlyphRasterizer();
            var defaultFont = new FontFaceKey(configuration.FontFamily, FontStyle.Regular);
            var defaultFontConfig = new FontConfiguration(
                defaultFont,
                italicFont: null,
                new PtFontSize(configuration.FontSize),
                defaultTextColor: RgbaFloat.White.ToVector4(),
                defaultOutlineColor: RgbaFloat.Black.ToVector4(),
                rubyFontSizeMultiplier: 0.4f
            );

            if (OperatingSystem.IsWindows())
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                await glyphRasterizer.AddFontAsync($"{windir}\\Fonts\\msgothic.ttc");
            }
            if (Directory.Exists("Fonts"))
            {
                await glyphRasterizer.AddFontsAsync(Directory.EnumerateFiles("Fonts"));
            }

            return (glyphRasterizer, defaultFontConfig);
        }

        private static AudioContext InitAudio(Configuration configuration)
        {
            var audioParameters = AudioParameters.Default;
            AudioBackend backend = configuration.PreferredAudioBackend
                ?? AudioDevice.GetPlatformDefaultBackend();
            var audioDevice = AudioDevice.Create(backend, audioParameters);
            return new AudioContext(audioDevice);
        }

        private static (GraphicsDevice device, Swapchain swapchain) InitGraphics(
            GameWindow window,
            Configuration configuration)
        {
            var options = new GraphicsDeviceOptions(false, null, configuration.EnableVSync);
            options.PreferStandardClipSpaceYDirection = true;
#if DEBUG
            options.Debug = true;
#endif
            GraphicsBackend backend = configuration.PreferredGraphicsBackend
                ?? VeldridStartup.GetPlatformDefaultBackend();
            var swapchainDesc = new SwapchainDescription(window.SwapchainSource,
                (uint)configuration.WindowWidth, (uint)configuration.WindowHeight,
                options.SwapchainDepthFormat, options.SyncToVerticalBlank);

            if (backend is GraphicsBackend.OpenGL or GraphicsBackend.OpenGLES)
            {
                var wnd = window as DesktopWindow;
                GraphicsDevice glDevice = backend == GraphicsBackend.OpenGL
                    ? VeldridStartup.CreateDefaultOpenGLGraphicsDevice(options, wnd!.SdlWindow, backend)
                    : GraphicsDevice.CreateOpenGLES(options, swapchainDesc);
                return (glDevice, glDevice.MainSwapchain);
            }

            GraphicsDevice device = backend switch
            {
                GraphicsBackend.Direct3D11 => GraphicsDevice.CreateD3D11(options),
                GraphicsBackend.Vulkan => GraphicsDevice.CreateVulkan(options),
                _ => ThrowHelper.Unreachable<GraphicsDevice>()
            };

            Swapchain swapchain = device.ResourceFactory.CreateSwapchain(ref swapchainDesc);
            return (device, swapchain);
        }

        private static ContentManager CreateContentManager(
            GraphicsDevice device,
            Configuration configuration)
        {
            TextureLoader textureLoader;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && true)
            {
                textureLoader = new WicTextureLoader(device);
            }
            else
            {
                textureLoader = null!;
                //textureLoader = new FFmpegTextureLoader(_graphicsDevice);
            }

            var content = new ContentManager(configuration.ContentRoot, textureLoader);
            return content;
        }

        private static (NsScriptVM vm, GameProcess mainProcess) LoadStartupScript(
            Configuration configuration,
            FontConfiguration fontConfig,
            Logger logger)
        {
            const string globalsFileName = "_globals";

            string nssFolder = Path.Combine(configuration.ContentRoot, configuration.ScriptRoot);
            string bytecodeCacheDir = nssFolder.Replace("nss", "nsx");

            string globalsPath = Path.Combine(bytecodeCacheDir, globalsFileName);
            if (configuration.SkipUpToDateCheck || !File.Exists(globalsPath)
                || !ValidateBytecodeCache(nssFolder, bytecodeCacheDir, configuration.SysScripts.Startup))
            {
                if (!Directory.Exists(bytecodeCacheDir))
                {
                    Directory.CreateDirectory(bytecodeCacheDir);
                    logger.LogInformation("Bytecode cache is empty. Compiling the scripts...");
                }
                else
                {
                    logger.LogInformation("Bytecode cache is not up-to-date. Recompiling the scripts...");
                    foreach (string file in Directory
                        .EnumerateFiles(bytecodeCacheDir, "*.nsx", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }

                Encoding? sourceEncoding = configuration.UseUtf8 ? Encoding.UTF8 : null;
                var compilation = new Compilation(
                    nssFolder,
                    bytecodeCacheDir,
                    globalsFileName,
                    sourceEncoding
                );

                SystemScripts sysScripts = configuration.SysScripts;
                string[] moduleNames =
                {
                    sysScripts.Startup,
                    sysScripts.Backlog, sysScripts.Menu,
                    sysScripts.Load, sysScripts.Save
                };

                SourceModuleSymbol[] modules = moduleNames
                    .Select(x => compilation.GetSourceModule(x))
                    .ToArray();

                compilation.Emit(modules);
            }
            else
            {
                logger.LogInformation("Bytecode cache is up-to-date.");
            }

            var nsxLocator = new FileSystemNsxModuleLocator(bytecodeCacheDir);
            var vm = new NsScriptVM(nsxLocator, File.OpenRead(globalsPath));
            GameProcess process = CreateProcess(vm, configuration.SysScripts.Startup, fontConfig);
            return (vm, process);
        }

        private static GameProcess CreateProcess(
            NsScriptVM vm,
            string modulePath,
            FontConfiguration fontConfig)
        {
            string moduleName = Path.ChangeExtension(modulePath, null);
            return new GameProcess(vm.CreateProcess(moduleName, "main"), fontConfig.Clone());
        }

        private static bool ValidateBytecodeCache(
            string nssFolder,
            string bytecodeCacheDir,
            string startupScript)
        {
            static string getModulePath(string rootDir, string fullPath)
            {
                return Path.ChangeExtension(
                    Path.GetRelativePath(rootDir, fullPath),
                    extension: null
                );
            }

            string startupModule = getModulePath(
                rootDir: nssFolder,
                Path.Combine(nssFolder, startupScript)
            );
            foreach (string nssFile in Directory
                .EnumerateFiles(nssFolder, "*.nss", SearchOption.AllDirectories))
            {
                string currentModule = getModulePath(rootDir: nssFolder, nssFile);
                string nsxPath = Path.ChangeExtension(
                    Path.Combine(bytecodeCacheDir, currentModule),
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

        public Task Run(bool useDedicatedThread = false)
        {
            if (useDedicatedThread)
            {
                return Task.Factory.StartNew(
                    MainLoop,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                );
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
            VM.SystemVariables.SavePath = ConstantValue.String(SaveManager.SaveDirectory);

            long prevFrameTicks = 0L;
            long frameId = 0L;

            bool surfaceDestroyed = false;
            bool needsResize = false;
            Window.Mobile_SurfaceDestroyed += () => surfaceDestroyed = true;
            Window.Resized += () => needsResize = true;

            while (!ShutdownSignal.IsCancellationRequested && Window.Exists)
            {
                long currentFrameTicks = Clock.ElapsedTicks;
                float deltaMilliseconds = (float)(currentFrameTicks - prevFrameTicks)
                    / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                try
                {
                    var framestamp = new FrameStamp(frameId++, Clock.ElapsedTicks);
                    Tick(framestamp, deltaMilliseconds);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        // Note to self: do not reoder anything inside the main loop unless absolutely
        // necessary or until the engine is both feature-complete and stable.
        private void Tick(FrameStamp framestamp, float dt)
        {
            RenderContext.BeginFrame(framestamp, _clearFramebuffer);
            _clearFramebuffer = true;
            InputContext.Update(VM.SystemVariables);
            Advance = InputContext.VKeyDown(VirtualKey.Advance);

            while (true)
            {
                GameProcess activeProcess = ActiveProcess;
                RunResult runResult = VM.Run(
                    activeProcess.VmProcess,
                    _builtInFunctions,
                    ShutdownSignal.Token
                );

                foreach (uint thread in runResult.TerminatedThreads)
                {
                    activeProcess.World.DestroyContext(thread);
                }

                ProcessSystemVariables(VM.SystemVariables);

                if (SysProcess is GameProcess { VmProcess: { IsTerminated: true } } sysProc)
                {
                    sysProc.Dispose();
                    SysProcess = null;
                    MainProcess.VmProcess.Resume();
                    continue;
                }

                break;
            }

            bool useHandCursor = ActiveProcess.World.Exists(FocusedUiElement);
            Window.SetCursor(useHandCursor ? SystemCursor.Hand : SystemCursor.Arrow);

            World world = ActiveProcess.World;
            bool assetsReady = Content.ResolveAssets();
            if (assetsReady)
            {
                world.BeginFrame();
            }

            ProcessSounds(world, dt);
            RenderFrame(framestamp, world.RenderItems, dt, assetsReady);
            HandleInput();
            RunDeferredOperations();

            if (assetsReady)
            {
                ActiveProcess.ProcessWaitOperations(this);
            }
            try
            {
                RenderContext.EndFrame();
                RenderContext.Present();
            }
            catch (VeldridException e)
                when (e.Message == "The Swapchain's underlying surface has been lost.")
            {
            }
        }

        private void RenderFrame(
            in FrameStamp frameStamp,
            SortableEntityGroupView<RenderItem> renderItems,
            float dt,
            bool assetsReady)
        {
            ReadOnlySpan<RenderItem> active = renderItems.SortActive();
            ReadOnlySpan<RenderItem> inactive = renderItems.Disabled;
            foreach (RenderItem ri in active)
            {
                ri.Update(this, dt, assetsReady);
            }
            foreach (RenderItem ri in inactive)
            {
                ri.Update(this, dt, assetsReady);
            }

            RenderContext.ResolveGlyphs();

            foreach (RenderItem ri in active)
            {
                ri.Render(RenderContext, assetsReady);
            }

            RenderContext.TextureCache.BeginFrame(frameStamp);
        }

        private static void ProcessSounds(World world, float dt)
        {
            foreach (Sound sound in world.Sounds.Enabled)
            {
                sound.Update(dt);
            }
        }

        private void ProcessSystemVariables(SystemVariableLookup sysVars)
        {
            Skipping = sysVars.Skip.AsBool()!.Value;
        }

        private void HandleInput()
        {
            if (InputContext.VKeyDown(VirtualKey.Left))
            {
                RequestedFocusChange = NsFocusDirection.Left;
            }
            else if (InputContext.VKeyDown(VirtualKey.Up))
            {
                RequestedFocusChange = NsFocusDirection.Up;
            }
            else if (InputContext.VKeyDown(VirtualKey.Right))
            {
                RequestedFocusChange = NsFocusDirection.Right;
            }
            else if (InputContext.VKeyDown(VirtualKey.Down))
            {
                RequestedFocusChange = NsFocusDirection.Down;
            }

            SystemVariableLookup sysVars = VM.SystemVariables;
            if (sysVars.MenuLock.AsBool() is not true)
            {
                if (InputContext.VKeyDown(VirtualKey.Back))
                {
                    CreateSysProcess(Config.SysScripts.Menu);
                    return;
                }
                if (sysVars.BacklogLock.AsBool() is not true)
                {
                    if (InputContext.WheelDelta > 0)
                    {
                        CreateSysProcess(Config.SysScripts.Backlog);
                        return;
                    }
                }
                if (sysVars.SkipLock.AsBool() is not true)
                {
                    if (InputContext.VKeyDown(VirtualKey.Skip))
                    {
                        Skipping = !Skipping;
                        VM.SystemVariables.Skip = ConstantValue.Boolean(Skipping);
                    }
                }
            }
        }

        private void CreateSysProcess(string mainModule)
        {
            //_context.SysProcess?.Dispose();
            if (SysProcess is null)
            {
                LastScreenshot ??= RenderContext.CreateFullscreenTexture(staging: true);
                RenderContext.CaptureFramebuffer(LastScreenshot);
                MainProcess.VmProcess.Suspend();
                SysProcess = CreateProcess(VM, mainModule, _fontConfig);
                _clearFramebuffer = false;
            }
        }

        private void RunDeferredOperations()
        {
            bool resumeProcess = _deferredOperations.Count > 0;
            while (_deferredOperations.TryDequeue(out DeferredOperation op))
            {
                switch (op.Kind)
                {
                    case DeferredOperationKind.CaptureFramebuffer:
                        Debug.Assert(op.ScreenshotTexture is not null);
                        RenderContext.CaptureFramebuffer(op.ScreenshotTexture);
                        break;
                    case DeferredOperationKind.SaveGame:
                        Debug.Assert(op.SaveSlot is not null);
                        SaveManager.Save(this, op.SaveSlot.Value);
                        break;
                    case DeferredOperationKind.LoadGame:
                        Debug.Assert(op.SaveSlot is not null);
                        SaveManager.Load(this, op.SaveSlot.Value);
                        break;
                }
            }

            if (resumeProcess)
            {
                ActiveProcess.VmProcess.Resume();
            }
        }

        internal void Defer(in DeferredOperation operation)
        {
            ActiveProcess.VmProcess.Suspend();
            _deferredOperations.Enqueue(operation);
        }

        internal void Wait(
            NsScriptThread thread,
            WaitCondition condition,
            TimeSpan? timeout = null,
            EntityQuery? entityQuery = null)
        {
            ActiveProcess.Wait(thread, condition, timeout, entityQuery);
        }

        internal void PlayVoice(string characterName, string filePath)
        {
            if (Skipping) { return; }
            if (Content.TryOpenStream($"voice/{filePath}") is Stream file)
            {
                if (_activeVoice is (string character, MediaStream prevVoice))
                {
                    _voices.Remove(character);
                    prevVoice.Dispose();
                }

                var voice = new MediaStream(
                    file,
                    graphicsDevice: null,
                    AudioContext.VoiceAudioSource,
                    AudioContext.Device.AudioParameters
                );
                voice.Start();
                _voices[characterName] = voice;
                _activeVoice = (characterName, voice);
            }
        }

        internal void StopVoice()
        {
            if (_activeVoice is (string character, MediaStream voice))
            {
                voice.Dispose();
                _voices.Remove(character);
                _activeVoice = default;
            }
        }

        internal MediaStream? GetVoice(string characterName)
            => _voices.TryGetValue(characterName, out MediaStream? voice) ? voice : null;

        public ValueTask DisposeAsync()
        {
            ValueTask destroyAudio = AudioContext.DisposeAsync();
            RenderContext.GraphicsDevice.WaitForIdle();
            InputContext.Dispose();
            MainProcess.Dispose();
            SysProcess?.Dispose();
            Content.Dispose();
            GlyphRasterizer.Dispose();
            RenderContext.Dispose();
            Window.Dispose();
            return destroyAudio;
        }

        public void Reset()
        {
            SysProcess?.Dispose();
            SysProcess = null;
            MainProcess.Dispose();
            MainProcess = CreateProcess(VM, Config.SysScripts.Startup, _fontConfig);
        }
    }
}
