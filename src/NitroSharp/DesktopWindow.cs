using System;
using System.Threading;
using System.Threading.Tasks;
using NitroSharp.Primitives;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NitroSharp
{
    public class DesktopWindow : GameWindow
    {
        private readonly Sdl2Window _window;

        public DesktopWindow(string title, uint width, uint height)
        {
            _window = new Sdl2Window(title, 100, 100, (int)width, (int)height, SDL_WindowFlags.OpenGL, false);
            _window.LimitPollRate = true;
            _window.PollIntervalInMs = 10.0f;
            SwapchainSource = VeldridStartup.GetSwapchainSource(_window);
        }

        public Sdl2Window SdlWindow => _window;
        public SwapchainSource SwapchainSource { get; }
        public Size Size => new Size((uint)_window.Width, (uint)_window.Height);
        public bool Exists => _window.Exists;

        public AutoResetEvent Mobile_HandledSurfaceDestroyed => throw new NotImplementedException();

        public event Action Resized;
        public event Action<SwapchainSource> Mobile_SurfaceCreated
        {
            add => Task.Run(() => value?.Invoke(SwapchainSource));
            remove => throw new NotImplementedException();
        }
        public event Action Mobile_SurfaceDestroyed;

        public InputSnapshot PumpEvents() => _window.PumpEvents();
    }
}
