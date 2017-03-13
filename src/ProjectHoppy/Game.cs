using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public abstract class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer = new Stopwatch();

        protected RenderContext RenderContext { get; private set; }
        public Window Window { get; private set; }
        public EntityManager Entities { get; }
        public SystemManager Systems { get; }

        public Game()
        {
            Entities = new EntityManager();
            Systems = new SystemManager(Entities);
        }

        public virtual Task Initialize()
        {
            Window = new GameWindow();
            Window.WindowState = WindowState.Normal;
            RenderContext = RenderContext.Create(GraphicsBackend.DirectX, Window);
            return Task.FromResult(0);
        }

        public virtual void Update(float deltaMilliseconds)
        {
            Systems.Update(deltaMilliseconds);
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
                prevFrameTicks = currentFrameTicks;

                Window.ProcessEvents();
                Update(deltaMilliseconds);
            }

            Shutdown();
        }

        public virtual Task Run()
        {
            return Initialize().ContinueWith(t => EnterLoop(), TaskContinuationOptions.ExecuteSynchronously);
        }

        public virtual void Shutdown()
        {
            Systems.Dispose();
            RenderContext.Dispose();
        }
    }
}
