using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProjectHoppy
{
    public abstract class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer = new Stopwatch();

        protected RenderContext RenderContext { get; private set; }
        protected ContentManager Content { get; private set; }
        public Window Window { get; }
        public EntityManager Entities { get; }
        public SystemManager Systems { get; }

        public Game()
        {
            Window = new GameWindow();
            Window.WindowState = WindowState.Normal;
            RenderContext = RenderContext.Create(GraphicsBackend.DirectX, Window);

            Entities = new EntityManager();
            Systems = new SystemManager(Entities);
        }

        public virtual ContentManager CreateContentManager()
        {
            return new ContentManager(RenderContext.ResourceFactory);
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

        public abstract void Run();

        public virtual void Shutdown()
        {
            Systems.Dispose();
            RenderContext.Dispose();
        }
    }
}
