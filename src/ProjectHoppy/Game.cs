using ProjectHoppy.Audio;
using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer.Platform;
using System;
using System.Diagnostics;
using System.Threading;

namespace ProjectHoppy
{
    public abstract class Game
    {
        private volatile bool _interacting;

        public Window Window { get; }
        public abstract ContentManager Content { get; }
        public GraphicsSystem Graphics { get; }
        public InputSystem Input { get; }
        public AudioSystem Audio { get; }

        public Game()
        {
            Window = new GameWindow();
            Window.WindowState = WindowState.Normal;
            Graphics = new GraphicsSystem(Window);
            Input = new InputSystem(this);
        }

        public abstract void Run();

        public void Interact(TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            _interacting = true;
            while (Window.Exists && _interacting)
            {
                if (sw.Elapsed >= timeout)
                {
                    sw.Stop();
                    return;
                }

                Window.ProcessEvents();
                Input.Update();
                Graphics.Update();

                Graphics.Render();
            }
        }

        public void StopInteracting()
        {
            _interacting = false;
        }
    }
}
