using System;
using System.Numerics;
using System.Threading;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NitroSharp
{
    public sealed class DesktopWindow : GameWindow
    {
        private readonly Sdl2Window _window;
        private IntPtr _hand;
        private IntPtr _arrow;
        private IntPtr _wait;

        public DesktopWindow(string title, uint width, uint height)
        {
            const int centered = Sdl2Native.SDL_WINDOWPOS_CENTERED;
            _window = new Sdl2Window(title,
                centered, centered,
                (int)width, (int)height,
                SDL_WindowFlags.OpenGL,
                threadedProcessing: false
            );
            SwapchainSource = VeldridStartup.GetSwapchainSource(_window);

            _arrow = Sdl2Native.SDL_CreateSystemCursor(SDL_SystemCursor.Arrow);
            _hand = Sdl2Native.SDL_CreateSystemCursor(SDL_SystemCursor.Hand);
            _wait = Sdl2Native.SDL_CreateSystemCursor(SDL_SystemCursor.Wait);
        }

        public SwapchainSource SwapchainSource { get; }
        public Size Size => new((uint)_window.Width, (uint)_window.Height);
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

        public void SetCursor(SystemCursor cursor)
        {
            IntPtr sdlCursor = cursor switch
            {
                SystemCursor.Hand => _hand,
                SystemCursor.Wait => _wait,
                _ => _arrow
            };
            Sdl2Native.SDL_SetCursor(sdlCursor);
        }

        public void Dispose()
        {
            Sdl2Native.SDL_FreeCursor(_wait);
            Sdl2Native.SDL_FreeCursor(_hand);
            Sdl2Native.SDL_FreeCursor(_arrow);
            _wait = IntPtr.Zero;
            _hand = IntPtr.Zero;
            _arrow = IntPtr.Zero;
        }
    }
}
