using NitroSharp.Foundation.Audio;
using NitroSharp.Foundation.Audio.XAudio;
using NitroSharp.Foundation.Content;
using NitroSharp.Foundation.Graphics;
using NitroSharp.Foundation.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NitroSharp.Foundation
{
    public abstract class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer;
        private GameParameters _parameters;

        protected Game()
        {
            FFmpegLibraries.Locate();

            _gameTimer = new Stopwatch();
            Entities = new EntityManager(_gameTimer);
            Systems = new SystemManager(Entities);
            MainLoopTaskScheduler = new MainLoopTaskScheduler(Environment.CurrentManagedThreadId);
            ShutdownCancellation = new CancellationTokenSource();
        }

        public DxRenderContext RenderContext { get; private set; }
        public AudioEngine AudioEngine { get; private set; }
        public ContentManager Content { get; private set; }
        public Window Window { get; private set; }
        public EntityManager Entities { get; }
        public SystemManager Systems { get; }

        protected bool Running => _running;
        protected CancellationTokenSource ShutdownCancellation { get; }
        public MainLoopTaskScheduler MainLoopTaskScheduler { get; }

        protected virtual void SetParameters(GameParameters parameters)
        {
            parameters.WindowWidth = 800;
            parameters.WindowHeight = 600;
            parameters.WindowTitle = "Sample Text";
        }

        protected virtual void RegisterStartupTasks(IList<Action> tasks)
        {
        }

        protected virtual ContentManager CreateContentManager()
        {
            return new ContentManager("Content");
        }

        protected virtual void RegisterSystems(IList<GameSystem> systems)
        {
        }

        public void Run()
        {
            _running = true;
            _gameTimer.Start();
            _parameters = new GameParameters();
            SetParameters(_parameters);

            var startupTasks = new List<Action>();
            RegisterStartupTasks(startupTasks);

            var startup = Task.WhenAll(startupTasks.Select(x => Task.Run(x)));
            InitializeGraphicsAndSound();

            startup.Wait();
            OnInitialized().Wait();
            RunMainLoop();
        }

        public void RunMainLoop()
        {
            const float desiredFrameTime = 1000.0f / 60.0f;

            float prevFrameTicks = 0.0f;
            while (_running && Window.Exists)
            {
                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;

                while (deltaMilliseconds < desiredFrameTime)
                {
                    currentFrameTicks = _gameTimer.ElapsedTicks;
                    deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                }

                prevFrameTicks = currentFrameTicks;
                Update(deltaMilliseconds);
            }

            Shutdown();
        }

        public virtual Task OnInitialized() => Task.FromResult(0);

        public virtual void Update(float deltaMilliseconds)
        {
            MainLoopTaskScheduler.FlushQueuedTasks();
            Systems.Update(deltaMilliseconds);
        }

        public void Exit()
        {
            _running = false;
            ShutdownCancellation.Cancel();
        }

        public virtual void Shutdown()
        {
            Systems.Dispose();
            AudioEngine.Dispose();
            RenderContext.Dispose();
            Content.Dispose();
        }

        private void InitializeGraphicsAndSound()
        {
            Window = new DedicatedThreadWindow(_parameters.WindowTitle, _parameters.WindowWidth, _parameters.WindowHeight, WindowState.Normal);
            RenderContext = new DxRenderContext(Window, multithreaded: true, enableVSync: _parameters.EnableVSync);
            AudioEngine = new XAudio2AudioEngine(16, 44100, 2);
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
