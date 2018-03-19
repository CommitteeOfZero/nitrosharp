using NitroSharp.Animation;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Execution;
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
    public class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer;

        private readonly Configuration _configuration;
        private readonly string _nssFolder;

        private NsScriptInterpreter _nssInterpreter;
        private CoreLogic _coreLogic;
        private Task _interpreterProc;
        private volatile bool _nextStateReady = false;

        private InputSystem _inputHandler;

        public Game(Configuration configuration)
        {
            _configuration = configuration;
            _nssFolder = Path.Combine(configuration.ContentRoot, "nss");

            _gameTimer = new Stopwatch();
            Entities = new EntityManager(_gameTimer);
            Systems = new SystemManager(Entities);
            ShutdownCancellation = new CancellationTokenSource();
        }

        protected ContentManager Content { get; private set; }
        private protected EntityManager Entities { get; }
        private protected SystemManager Systems { get; }

        protected Sdl2Window Window { get; private set; }
        protected GraphicsDevice GraphicsDevice { get; private set; }
        private RenderSystem RenderSystem { get; set; }

        protected bool Running => _running;
        protected CancellationTokenSource ShutdownCancellation { get; }

        protected virtual void RegisterStartupTasks(IList<Action> tasks)
        {
            tasks.Add(() => LoadStartupScript());
        }

        protected virtual ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);

            ContentLoader textureLoader;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                textureLoader = new WicTextureLoader(GraphicsDevice);
            }
            else
            {
                textureLoader = new ImageSharpTextureLoader(GraphicsDevice);
            }

            content.RegisterContentLoader(typeof(BindableTexture), textureLoader);

            _coreLogic.SetContent(content);
            return content;
        }

        protected virtual void RegisterSystems(IList<GameSystem> systems)
        {
            _inputHandler = new InputSystem(Window, _coreLogic);

            var animationSystem = new AnimationSystem();
            systems.Add(animationSystem);

            RenderSystem = new RenderSystem(GraphicsDevice, _configuration);
            systems.Add(RenderSystem);
        }

        private void LoadStartupScript()
        {
            _coreLogic = new CoreLogic(this, Entities);
            _nssInterpreter = new NsScriptInterpreter(LocateScript, _coreLogic);
            _nssInterpreter.CreateThread("__MAIN", _configuration.StartupScript, "main");
        }

        private Stream LocateScript(SourceFileReference fileRef)
        {
            return File.OpenRead(Path.Combine(_nssFolder, fileRef.FilePath.Replace("nss/", string.Empty)));
        }

        protected virtual void OnInitialized()
        {
            _interpreterProc = Task.Factory.StartNew(() => RunInterpreterLoop(), TaskCreationOptions.LongRunning);
        }

        public void Run()
        {
            _running = true;
            _gameTimer.Start();

            var startupTasks = new List<Action>();
            RegisterStartupTasks(startupTasks);

            var startup = Task.WhenAll(startupTasks.Select(x => Task.Run(x)));
            InitializeGraphicsAndSound();

            startup.Wait();
            OnInitialized();
            RunMainLoop();
        }

        protected virtual void Update(float deltaMilliseconds)
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
                Systems.ProcessEntityUpdates();
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

            Systems.Update(deltaMilliseconds);
            RenderSystem.Present();
        }

        private void RunInterpreterLoop()
        {
            while (Running)
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
            //const float desiredFrameTime = 1000.0f / 60.0f;

            float prevFrameTicks = 0.0f;
            while (_running && Window.Exists)
            {
                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;

                //while (deltaMilliseconds < desiredFrameTime)
                //{
                //    currentFrameTicks = _gameTimer.ElapsedTicks;
                //    deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                //}

                prevFrameTicks = currentFrameTicks;
                Update(deltaMilliseconds);
            }

            DestroyResources();
        }

        public void Exit()
        {
            _running = false;
            ShutdownCancellation.Cancel();
        }

        public virtual void DestroyResources()
        {
            Systems.Dispose();
            Content.Dispose();
            GraphicsDevice.Dispose();
        }

        private void InitializeGraphicsAndSound()
        {
            Window = new Sdl2Window(
                _configuration.WindowTitle, 100, 100,
                _configuration.WindowWidth, _configuration.WindowHeight,
                SDL_WindowFlags.OpenGL, false);

            Window.LimitPollRate = true;

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(false, PixelFormat.R16_UNorm, true);
#if DEBUG
            options.Debug = true;
#endif
            GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Window, options);

            Content = CreateContentManager();

            var userSystems = new List<GameSystem>();
            RegisterSystems(userSystems);
            foreach (var system in userSystems)
            {
                Systems.Add(system);
            }
        }
    }
}
