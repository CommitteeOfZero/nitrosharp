using System;
using System.Numerics;
using System.Runtime.InteropServices;
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

            _arrow = Sdl2Cursor.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
            _hand = Sdl2Cursor.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
            _wait = Sdl2Cursor.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT);
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
            Sdl2Cursor.SDL_SetCursor(sdlCursor);
        }

        public void Dispose()
        {
            Sdl2Cursor.SDL_FreeCursor(_wait);
            Sdl2Cursor.SDL_FreeCursor(_hand);
            Sdl2Cursor.SDL_FreeCursor(_arrow);
            _wait = IntPtr.Zero;
            _hand = IntPtr.Zero;
            _arrow = IntPtr.Zero;
        }

        private enum SDL_SystemCursor
        {
            SDL_SYSTEM_CURSOR_ARROW,
            SDL_SYSTEM_CURSOR_IBEAM,
            SDL_SYSTEM_CURSOR_WAIT,
            SDL_SYSTEM_CURSOR_CROSSHAIR,
            SDL_SYSTEM_CURSOR_WAITARROW,
            SDL_SYSTEM_CURSOR_SIZENWSE,
            SDL_SYSTEM_CURSOR_SIZENESW,
            SDL_SYSTEM_CURSOR_SIZEWE,
            SDL_SYSTEM_CURSOR_SIZENS,
            SDL_SYSTEM_CURSOR_SIZEALL,
            SDL_SYSTEM_CURSOR_NO,
            SDL_SYSTEM_CURSOR_HAND,
            SDL_NUM_SYSTEM_CURSORS
        }

        private static class Sdl2Cursor
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate IntPtr SDL_CreateSystemCursor_t(SDL_SystemCursor id);
            private static readonly SDL_CreateSystemCursor_t s_sdl_createSystemCursor = Sdl2Native.LoadFunction<SDL_CreateSystemCursor_t>("SDL_CreateSystemCursor");
            public static IntPtr SDL_CreateSystemCursor(SDL_SystemCursor id) => s_sdl_createSystemCursor(id);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void SDL_SetCursor_t(IntPtr cursor);
            private static readonly SDL_SetCursor_t s_sdl_setCursor = Sdl2Native.LoadFunction<SDL_SetCursor_t>("SDL_SetCursor");
            public static void SDL_SetCursor(IntPtr cursor) => s_sdl_setCursor(cursor);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void SDL_FreeCursor_t(IntPtr cursor);
            private static readonly SDL_FreeCursor_t s_sdl_freeCursor = Sdl2Native.LoadFunction<SDL_FreeCursor_t>("SDL_FreeCursor");
            public static void SDL_FreeCursor(IntPtr cursor) => s_sdl_freeCursor(cursor);
        }
    }
}
