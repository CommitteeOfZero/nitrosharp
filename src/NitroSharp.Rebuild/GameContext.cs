using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Veldrid;
using Veldrid.StartupUtilities;
using ZeroLog;
using ZeroLog.Appenders;
using ZeroLog.Configuration;

[assembly: InternalsVisibleTo("NitroSharp.Tests")]

namespace NitroSharp;

internal readonly record struct FrameStamp(long FrameId, long StopwatchTicks)
{
    public static FrameStamp Invalid => new(-1, -1);
    public bool IsValid => FrameId >= 0 && StopwatchTicks >= 0;
}

internal sealed class GameContext
{
    public required GameWindow Window { get; init; }
    internal required GameProfile Profile { get; init; }
    internal required RenderContext RenderContext { get; init; }
    internal required ContentManager Content { get; init; }
    internal required GlyphRasterizer GlyphRasterizer { get; init; }
    internal required AudioContext AudioContext { get; init; }
    internal required InputContext InputContext { get; init; }
    internal required NsScriptVM VM { get; init; }
    internal Builtins Builtins { get; private set; }
    internal required World World { get; init; }
    internal Stopwatch Clock { get; } = Stopwatch.StartNew();
    internal CancellationTokenSource ShutdownSignal { get; } = new();

    internal float DeltaTime { get; private set; }
    internal FrameStamp FrameStamp { get; private set; }

    public void Run()
    {
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
                DeltaTime = deltaMilliseconds;
                FrameStamp = new FrameStamp(frameId++, Clock.ElapsedTicks);
                Tick();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private void Tick()
    {
        InputSnapshot inputSnapshot = Window.PumpEvents();
        World.BeginFrame();
        World.Update(this);
        if (Content.ResolveAssets())
        {
            RenderContext.BeginFrame(FrameStamp, clear: true);
            World.Render(this);
            RenderContext.EndFrame();
        }
        RenderContext.Present();
    }

    public static async Task<GameContext> Create(GameWindow window, Config config, GameProfile profile)
    {
        Log log = CreateLogger();
        log.Info("**** Start apprication ****");
        log.Info(
            $"""
             Game: {profile.ProductDisplayName}
             Profile: {profile.Name}
             """
        );

        var createSurface = new TaskCompletionSource<SwapchainSource>();
        window.Mobile_SurfaceCreated += surf => createSurface.SetResult(surf);
        var initAudio = Task.Run(() => InitAudio(config));
        var loadFonts = Task.Run(async () => await LoadFonts(profile));

        (GlyphRasterizer glyphRasterizer, FontSettings fontSettings) = await loadFonts;
        var startVM = Task.Run(() => LoadStartupScript(profile, log));

        SwapchainSource swapchainSource = await createSurface.Task;
        (GraphicsDevice gd, Swapchain swapchain) = InitGraphics(window, config);
        ContentManager contentMgr = CreateContentManager(gd, profile);
        AudioContext audioContext = await initAudio;
        (NsScriptVM vm, NsScriptThreadState mainThread) = await startVM;

        var inputContext = new InputContext(window);
        var renderContext = new RenderContext(
            window,
            config,
            profile,
            gd,
            swapchain,
            contentMgr,
            glyphRasterizer,
            null!
        );

        log.Info(
            $"""
            Init successful.
            Graphics backend: {gd.BackendType.ToString()}
            Audio backend: {audioContext.Device.Backend.ToString()}
            """
        );

        var world = new World();
        Process mainProcess = CreateProcess(world, vm, profile.SysScripts.Startup, profile, fontSettings);
        world.RegisterProcess(mainProcess, isMain: true, activate: true);
        var ctx =  new GameContext
        {
            Window = window,
            AudioContext = audioContext,
            Content = contentMgr,
            GlyphRasterizer = glyphRasterizer,
            Profile = profile,
            RenderContext = renderContext,
            InputContext = inputContext,
            VM = vm,
            World = world
        };
        ctx.Builtins = new Builtins(ctx);
        return ctx;
    }

    private static Log CreateLogger()
    {
        var consoleAppender = new ConsoleAppender { Formatter = new LogFormatter(), ColorOutput = true };
        LogManager.Initialize(new ZeroLogConfiguration { RootLogger = { Appenders = { consoleAppender } } });
        return LogManager.GetLogger("main");
    }

    private static async Task<(GlyphRasterizer, FontSettings)> LoadFonts(GameProfile gameProfile)
    {
        var glyphRasterizer = new GlyphRasterizer();
        var defaultFont = new FontFaceKey(gameProfile.FontFamily, FontStyle.Regular);
        var defaultFontSettings = new FontSettings
        {
            DefaultFont = defaultFont,
            ItalicFont = null,
            DefaultFontSize = gameProfile.FontSize,
            DefaultTextColor = RgbaFloat.White.ToVector4(),
            DefaultOutlineColor = RgbaFloat.Black.ToVector4(),
            RubyFontSizeMultiplier = 0.4f
        };

        if (OperatingSystem.IsWindows())
        {
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            await glyphRasterizer.AddFontAsync($"{windir}\\Fonts\\msgothic.ttc");
        }

        if (Directory.Exists("Fonts"))
        {
            await glyphRasterizer.AddFontsAsync(Directory.EnumerateFiles("Fonts"));
        }

        return (glyphRasterizer, defaultFontSettings);
    }

    private static AudioContext InitAudio(Config config)
    {
        var audioParameters = AudioParameters.Default;
        AudioBackend backend = AudioDevice.GetPlatformDefaultBackend();
        if (config.PreferredAudioBackend is { } preferredBackend && AudioDevice.IsBackendAvailable(preferredBackend))
        {
            backend = preferredBackend;
        }

        var audioDevice = AudioDevice.Create(backend, audioParameters);
        return new AudioContext(audioDevice);
    }

    private static (GraphicsDevice device, Swapchain swapchain) InitGraphics(GameWindow window, Config configuration)
    {
        var options = new GraphicsDeviceOptions(debug: false, swapchainDepthFormat: null, configuration.EnableVSync);
        options.PreferStandardClipSpaceYDirection = true;
#if DEBUG
        options.Debug = true;
#endif
        GraphicsBackend backend = configuration.PreferredGraphicsBackend ?? VeldridStartup.GetPlatformDefaultBackend();
        ScreenSizeU renderResolution = window.Size;
        var swapchainDesc = new SwapchainDescription(
            window.SwapchainSource,
            renderResolution.Width,
            renderResolution.Height,
            options.SwapchainDepthFormat,
            options.SyncToVerticalBlank
        );

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

    private static ContentManager CreateContentManager(GraphicsDevice device, GameProfile gameProfile)
    {
        TextureLoader textureLoader;
        if (OperatingSystem.IsWindows())
        {
            textureLoader = new WicTextureLoader(device);
        }
        else
        {
            textureLoader = new FFmpegTextureLoader(device);
        }

        var content = new ContentManager(gameProfile.ContentRoot, textureLoader, gameProfile.MountPoints);
        return content;
    }

    private static (NsScriptVM vm, NsScriptThreadState mainThread) LoadStartupScript(GameProfile gameProfile, Log log)
    {
        const string globalsFileName = "_globals";

        string nssFolder = Path.Combine(gameProfile.ContentRoot, gameProfile.ScriptRoot);
        string bytecodeCacheDir = nssFolder.Replace("nss", "nsx");

        string globalsPath = Path.Combine(bytecodeCacheDir, globalsFileName);
        if (gameProfile.SkipUpToDateCheck || !File.Exists(globalsPath))
        {
            if (!Directory.Exists(bytecodeCacheDir))
            {
                Directory.CreateDirectory(bytecodeCacheDir);
                log.Info("Bytecode cache is empty. Compiling the scripts...");
            }
            else
            {
                log.Info("Bytecode cache is not up-to-date. Recompiling the scripts...");
                foreach (string file in Directory
                    .EnumerateFiles(bytecodeCacheDir, "*.nsx", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
            }

            Encoding? sourceEncoding = null;
            if (!gameProfile.DetectEncoding)
            {
                sourceEncoding = gameProfile.UseUtf8
                    ? Encoding.UTF8
                    : SourceText.DefaultEncoding;
            }
            var compilation = new Compilation(
                nssFolder,
                bytecodeCacheDir,
                globalsFileName,
                sourceEncoding
            );

            SystemScripts sysScripts = gameProfile.SysScripts;
            string[] moduleNames =
            {
                sysScripts.Startup,
                sysScripts.Backlog, sysScripts.Menu,
                sysScripts.Load, sysScripts.Save
            };

            var nssFolderInfo = new DirectoryInfo(nssFolder);
            string[] existingModules = moduleNames.Where(x => nssFolderInfo.EnumerateFiles(x).Any()).ToArray();
            IEnumerable<string> nonExistingModules = moduleNames.Except(existingModules);

            foreach (string nonExistingModule in nonExistingModules)
            {
                log.Warn($"System module '{nonExistingModule}' is missing");
            }

            SourceModuleSymbol[] modules = existingModules
                .Select(x => compilation.GetSourceModule(x))
                .ToArray();

            compilation.Emit(modules);
        }
        else
        {
            log.Info("Bytecode cache is up-to-date.");
        }

        var nsxLocator = new FileSystemNsxModuleLocator(bytecodeCacheDir);
        var vm = new NsScriptVM(nsxLocator, File.OpenRead(globalsPath));
        NsScriptThreadState mainThread = CreateThread(vm, gameProfile.SysScripts.Startup);
        return (vm, mainThread);
    }

    private static Process CreateProcess(World world, NsScriptVM vm, string modulePath, GameProfile profile, FontSettings fontSettings)
    {
        string fullModulePath = Path.Combine(profile.ScriptRoot, modulePath);
        var processName = EntityName.Parse(Path.GetFileNameWithoutExtension(modulePath));
        NsScriptThreadState threadState = CreateThread(vm, modulePath);
        var process = new Process(processName, parent: null, fontSettings);
        var mainThread = new Thread(EntityName.Parse("main"), parent: process, threadState, isMain: true);
        world.AddEntity(process);
        world.AddEntity(mainThread);
        return process;
    }

    private static NsScriptThreadState CreateThread(NsScriptVM vm, string modulePath)
    {
        string moduleName = Path.ChangeExtension(modulePath, null);
        return vm.CreateThread(moduleName, "main")!.Value;
    }
}
