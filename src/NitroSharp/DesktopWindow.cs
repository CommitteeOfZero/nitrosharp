using System;
using System.Numerics;
using System.Threading;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

#nullable enable

namespace NitroSharp
{
    public sealed class DesktopWindow : GameWindow
    {
        private readonly Sdl2Window _window;

        public DesktopWindow(string title, uint width, uint height)
        {
            const int centered = Sdl2Native.SDL_WINDOWPOS_CENTERED;
            _window = new Sdl2Window(
                title,
                centered, centered,
                (int)width, (int)height,
                SDL_WindowFlags.OpenGL,
                threadedProcessing: false)
            {
                LimitPollRate = true,
                PollIntervalInMs = 10.0f
            };
            SwapchainSource = VeldridStartup.GetSwapchainSource(_window);
        }

        public SwapchainSource SwapchainSource { get; }
        public Size Size => new Size((uint)_window.Width, (uint)_window.Height);
        public bool Exists => _window.Exists;

        public AutoResetEvent Mobile_HandledSurfaceDestroyed => throw new NotImplementedException();

        public event Action? Resized;
        public event Action<SwapchainSource>? Mobile_SurfaceCreated
        {
            add => value?.Invoke(SwapchainSource);
            remove => throw new NotImplementedException();
        }
        public event Action? Mobile_SurfaceDestroyed;

        public InputSnapshot PumpEvents() => _window.PumpEvents();
        public void SetMousePosition(Vector2 pos) => _window.SetMousePosition(pos);
    }
}
