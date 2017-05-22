using MoeGame.Framework.Audio;
using MoeGame.Framework.Audio.XAudio;
using MoeGame.Framework.Content;
using MoeGame.Framework.Graphics;
using MoeGame.Framework.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MoeGame.Framework
{
    public abstract class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer;

        private GameParameters _parameters;

        public Game()
        {
            _gameTimer = new Stopwatch();
            Entities = new EntityManager(_gameTimer);
            Systems = new SystemManager(Entities);
            MainLoopTaskScheduler = new MainLoopTaskScheduler(Environment.CurrentManagedThreadId);
        }

        public DxRenderContext RenderContext { get; private set; }
        public AudioEngine AudioEngine { get; private set; }
        public ContentManager Content { get; private set; }
        public Window Window { get; private set; }
        public EntityManager Entities { get; }
        public SystemManager Systems { get; }

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
            _parameters = new GameParameters();
            SetParameters(_parameters);

            var startupTasks = new List<Action>();
            RegisterStartupTasks(startupTasks);

            var startup = Task.WhenAll(startupTasks.Select(x => Task.Run(x)));
            InitializeGraphicsAndSound();
            startup.Wait();

            EnterLoop();
        }

        public void EnterLoop()
        {
            _running = true;
            _gameTimer.Start();

            float prevFrameTicks = 0.0f;
            while (_running && Window.Exists)
            {
                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;

                //while (deltaMilliseconds < 1000.0f / 60.0f)
                //{
                //    Thread.Sleep(0);
                //    currentFrameTicks = _gameTimer.ElapsedTicks;
                //    deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                //}

                prevFrameTicks = currentFrameTicks;

                Window.ProcessEvents();
                Update(deltaMilliseconds);
            }

            Shutdown();
        }

        public virtual void LoadCommonResources()
        {
        }

        public virtual void Update(float deltaMilliseconds)
        {
            MainLoopTaskScheduler.FlushQueuedTasks();
            Systems.Update(deltaMilliseconds);
        }

        public virtual void Shutdown()
        {
            Systems.Dispose();
            AudioEngine.Dispose();
            RenderContext.Dispose();
        }

        private void InitializeGraphicsAndSound()
        {
            Window = new GameWindow(_parameters.WindowTitle, _parameters.WindowWidth, _parameters.WindowHeight, WindowState.Normal);
            RenderContext = new DxRenderContext(Window, _parameters.EnableVSync);
            AudioEngine = new XAudio2AudioEngine(16, 44100, 2);
            Content = CreateContentManager();
            Window.ProcessEvents();

            var userSystems = new List<GameSystem>();
            RegisterSystems(userSystems);
            foreach (var system in userSystems)
            {
                Systems.Add(system);
            }

            LoadCommonResources();
        }
    }
}
