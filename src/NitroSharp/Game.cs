using NitroSharp.Animation;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Execution;
using NitroSharp.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NitroSharp
{
    public sealed class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer;
        private readonly CancellationTokenSource _shutdownCancellation;
        
        private Sdl2Window _window;
        private GraphicsDevice _graphicsDevice;
        private readonly SystemManager _systems;
        private RenderSystem _renderSystem;
        private InputSystem _inputHandler;
        private readonly Configuration _configuration;
        
        private readonly string _nssFolder;
        private NsScriptInterpreter _nssInterpreter;
        private readonly CoreLogic _coreLogic;
        private Task _interpreterProc;
        private volatile bool _nextStateReady = false;

        private SharpDX.WIC.ImagingFactory _wicFactory;

        public Game(Configuration configuration)
        {
            _configuration = configuration;
            _nssFolder = Path.Combine(configuration.ContentRoot, "nss");

            _gameTimer = new Stopwatch();
            var entities = new EntityManager(_gameTimer);
            _systems = new SystemManager(entities);
            _shutdownCancellation = new CancellationTokenSource();
            _coreLogic = new CoreLogic(this, entities);
        }

        internal ContentManager Content { get; private set; }
        internal FontService FontService { get; private set; }

        public void Run()
        {
            _running = true;
            _gameTimer.Start();

            var loadScriptTask = Task.Run((Action)LoadStartupScript);
            Initialize();

            loadScriptTask.Wait();
            OnInitialized();
            RunMainLoop();
        }

        private void Initialize()
        {
            _window = new Sdl2Window(
                _configuration.WindowTitle, 100, 100,
                _configuration.WindowWidth, _configuration.WindowHeight,
                SDL_WindowFlags.OpenGL, false);

            _window.LimitPollRate = true;

            var options = new GraphicsDeviceOptions(false, PixelFormat.R16_UNorm, true);
#if DEBUG
            options.Debug = true;
#endif
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options);

            Content = CreateContentManager();
            FontService = CreateFontService();

            var userSystems = new List<GameSystem>();
            RegisterSystems(userSystems);
            foreach (var system in userSystems)
            {
                _systems.Add(system);
            }
        }

        private ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);
            ContentLoader textureLoader = null;
            ContentLoader textureDataLoader = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _wicFactory = new SharpDX.WIC.ImagingFactory();
                textureLoader = new WicTextureLoader(_wicFactory, _graphicsDevice);
                textureDataLoader = new WicTextureDataLoader(_wicFactory);
            }
            else
            {
                throw new Exception("Non-Windows platforms are temporarily not supported due to issues with ImageSharp.");
                //textureLoader = new ImageSharpTextureLoader(GraphicsDevice);
            }

            content.RegisterContentLoader(typeof(BindableTexture), textureLoader);
            content.RegisterContentLoader(typeof(TextureData), textureDataLoader);
            return content;
        }

        private FontService CreateFontService()
        {
            var fontService = new FontService();
            fontService.RegisterFonts(Directory.EnumerateFiles("Fonts"));
            return fontService;
        }

        private void RegisterSystems(IList<GameSystem> systems)
        {
            _inputHandler = new InputSystem(_window, _coreLogic);

            var animationSystem = new AnimationSystem();
            systems.Add(animationSystem);

            _renderSystem = new RenderSystem(_graphicsDevice, FontService, _configuration);
            systems.Add(_renderSystem);
        }

        private void OnInitialized()
        {
            _coreLogic.InitializeResources();
            _interpreterProc = Task.Factory.StartNew(RunInterpreterLoop, TaskCreationOptions.LongRunning);
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

        private void RunInterpreterLoop()
        {
            while (_running)
            {
                while (_nextStateReady)
                {
                    Thread.Sleep(5);
                }

                _nssInterpreter.Run(CancellationToken.None);
                _nextStateReady = true;
            }
        }

        private void RunMainLoop()
        {
            float prevFrameTicks = 0.0f;
            while (!_shutdownCancellation.IsCancellationRequested && _window.Exists)
            {
                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;
                Update(deltaMilliseconds);
            }

            DestroyResources();
        }

        public void Exit()
        {
            _running = false;
            _shutdownCancellation.Cancel();
        }

        private void DestroyResources()
        {
            _systems.Dispose();
            FontService.Dispose();
            Content.Dispose();
            _graphicsDevice.Dispose();
            _wicFactory?.Dispose();
        }
    }
}
