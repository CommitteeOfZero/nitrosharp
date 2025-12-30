using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Input;
using SDL;
using Veldrid;
using static SDL.SDL3;

namespace NitroSharp;

public sealed unsafe class Sdl3Window : GameWindow
{
    private enum WindowSystem
    {
        Windows,
        X11,
        Wayland,
        Cocoa,
        Android
    }

    private readonly Sdl3InputSnapshot _inputSnapshot = new();

    private readonly SDL_Cursor* _arrow;
    private readonly SDL_Cursor* _hand;
    private readonly SDL_Cursor* _wait;
    private SystemCursor _cursor;

    private MouseButton _mouseDown;

    public Sdl3Window(string title, ScreenSizeU size, GraphicsBackend graphicsBackend)
    {
        SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD);
        SDL_WindowFlags flags = graphicsBackend switch
        {
            GraphicsBackend.Vulkan => SDL_WindowFlags.SDL_WINDOW_VULKAN,
            GraphicsBackend.OpenGL or GraphicsBackend.OpenGLES => SDL_WindowFlags.SDL_WINDOW_OPENGL,
            _ => 0
        };

        SdlWindow = SDL_CreateWindow(title, (int)size.Width, (int)size.Height, flags);
        WindowSystem windowSystem = GetWindowSystem();
        SwapchainSource = GetSwapchainSource(windowSystem);
        Size = size;

        _arrow = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);
        _hand = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER);
        _wait = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT);
        _cursor = SystemCursor.Arrow;
        Exists = true;
    }

    public SDL_Window* SdlWindow { get; }
    public SwapchainSource SwapchainSource { get; }
    public ScreenSizeU Size { get; private set; }
    public bool Exists { get; private set; }

    public event Action? CloseRequested;
    public event Action? Resized;

    public void SetMousePosition(Vector2 pos)
    {
        SDL_WarpMouseInWindow(SdlWindow, pos.X, pos.Y);
    }

    public void SetCursor(SystemCursor cursor)
    {
        if (cursor != _cursor)
        {
            SDL_Cursor* sdlCursor = cursor switch
            {
                SystemCursor.Hand => _hand,
                SystemCursor.Wait => _wait,
                _ => _arrow
            };
            SDL_SetCursor(sdlCursor);
            _cursor = cursor;
        }
    }

    public InputSnapshot PumpEvents()
    {
        _inputSnapshot.Clear();
        SDL_Event ev;
        while (SDL_PollEvent(&ev))
        {
            switch ((SDL_EventType)ev.type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                    Exists = false;
                    CloseRequested?.Invoke();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                {
                    int w, h;
                    SDL_GetWindowSize(SdlWindow, &w, &h);
                    Size = new ScreenSizeU((uint)w, (uint)h);
                    Resized?.Invoke();
                    break;
                }

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL_EventType.SDL_EVENT_KEY_UP:
                {
                    bool down = (SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_KEY_DOWN;
                    _inputSnapshot.KeyEventList.Add(new KeyEvent(
                        (uint)ev.key.timestamp,
                        (uint)ev.key.windowID,
                        down,
                        ev.key.repeat,
                        (Key)ev.key.scancode,
                        (VKey)ev.key.key,
                        (ModifierKeys)ev.key.mod
                    ));
                    break;
                }

                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    _inputSnapshot.SetMousePosition(new Vector2(ev.motion.x, ev.motion.y));
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                {
                    bool down = (SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN;
                    var button = (MouseButton)ev.button.button;
                    _inputSnapshot.MouseEventList.Add(new MouseButtonEvent(
                        (uint)ev.button.timestamp,
                        (uint)ev.button.windowID,
                        button,
                        down,
                        ev.button.clicks
                    ));
                    if (down) { _mouseDown |= button; }
                    else { _mouseDown &= ~button; }
                    break;
                }

                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    _inputSnapshot.SetWheelDelta(new Vector2(ev.wheel.x, ev.wheel.y));
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    _inputSnapshot.GamepadEventList.Add(new GamepadEvent
                    {
                        Kind = GamepadEventKind.Added,
                        GamepadId = (uint)ev.gdevice.which
                    });
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    _inputSnapshot.GamepadEventList.Add(new GamepadEvent
                    {
                        Kind = GamepadEventKind.Removed,
                        GamepadId = (uint)ev.gdevice.which
                    });
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    _inputSnapshot.GamepadEventList.Add(new GamepadEvent
                    {
                        Kind = (SDL_EventType)ev.type == SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN
                            ? GamepadEventKind.ButtonDown
                            : GamepadEventKind.ButtonUp,
                        GamepadId = (uint)ev.gbutton.which,
                        Button = (GamepadButton)ev.gbutton.button
                    });
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    _inputSnapshot.GamepadEventList.Add(new GamepadEvent
                    {
                        Kind = GamepadEventKind.AxisMotion,
                        GamepadId = (uint)ev.gaxis.which,
                        Axis = (GamepadAxis)ev.gaxis.axis,
                        AxisValue = ev.gaxis.value < 0
                            ? -((float)ev.gaxis.value / short.MinValue)
                            : (float)ev.gaxis.value / short.MaxValue
                    });
                    break;

                case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                case SDL_EventType.SDL_EVENT_FINGER_UP:
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                {
                    TouchEventKind kind = (SDL_EventType)ev.type switch
                    {
                        SDL_EventType.SDL_EVENT_FINGER_DOWN => TouchEventKind.Down,
                        SDL_EventType.SDL_EVENT_FINGER_UP => TouchEventKind.Up,
                        _ => TouchEventKind.Motion
                    };
                    _inputSnapshot.TouchEventList.Add(new TouchEvent(
                        (uint)ev.tfinger.timestamp,
                        kind,
                        (ulong)ev.tfinger.fingerID,
                        new Vector2(ev.tfinger.x * Size.Width, ev.tfinger.y * Size.Height),
                        ev.tfinger.pressure
                    ));
                    break;
                }
            }
        }

        _inputSnapshot.Refresh();
        return _inputSnapshot;
    }

    private static WindowSystem GetWindowSystem()
    {
        if (OperatingSystem.IsWindows()) { return WindowSystem.Windows; }
        if (OperatingSystem.IsMacOS()) { return WindowSystem.Cocoa; }
        if (OperatingSystem.IsAndroid()) { return WindowSystem.Android; }

        if (OperatingSystem.IsLinux())
        {
            string? videoDriver = SDL_GetCurrentVideoDriver();
            return videoDriver switch
            {
                "wayland" => WindowSystem.Wayland,
                "x11" => WindowSystem.X11,
                _ => throw new Exception($"Unexpected SDL video driver '{videoDriver}'")
            };
        }

        throw new PlatformNotSupportedException();
    }

    private SwapchainSource GetSwapchainSource(WindowSystem windowSystem)
    {
        SDL_PropertiesID props = SDL_GetWindowProperties(SdlWindow);
        switch (windowSystem)
        {
            case WindowSystem.Windows:
            {
                IntPtr hwnd = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero);
                IntPtr hInstance = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_WIN32_INSTANCE_POINTER, 0);
                return SwapchainSource.CreateWin32(hwnd, hInstance);
            }
            case WindowSystem.X11:
            {
                IntPtr window = new(SDL_GetNumberProperty(props, SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0));
                IntPtr display = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_X11_DISPLAY_POINTER, IntPtr.Zero);
                return SwapchainSource.CreateXlib(display, window);
            }
            case WindowSystem.Wayland:
            {
                IntPtr surface = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_WAYLAND_SURFACE_POINTER, IntPtr.Zero);
                IntPtr display = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, 0);
                return SwapchainSource.CreateWayland(display, surface);
            }
            case WindowSystem.Android:
            {
                IntPtr window = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_ANDROID_WINDOW_POINTER, IntPtr.Zero);
                return SwapchainSource.CreateAndroidWindow(window);
            }
            case WindowSystem.Cocoa:
            {
                IntPtr nsWindow = SDL_GetPointerProperty(props, SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, IntPtr.Zero);
                return SwapchainSource.CreateNSWindow(nsWindow);
            }
            default:
            {
                throw ThrowHelper.UnexpectedValueOf<WindowSystem>();
            }
        }
    }

    public void Dispose()
    {
        SDL_DestroyCursor(_arrow);
        SDL_DestroyCursor(_hand);
        SDL_DestroyCursor(_wait);
        SDL_DestroyWindow(SdlWindow);
        SDL_Quit();
    }

    private sealed class Sdl3InputSnapshot : InputSnapshot
    {
        public List<KeyEvent> KeyEventList { get; } = [];
        public List<MouseButtonEvent> MouseEventList { get; } = [];
        public List<GamepadEvent> GamepadEventList { get; } = [];
        public List<TouchEvent> TouchEventList { get; } = [];

        public override ReadOnlySpan<KeyEvent> KeyEvents => CollectionsMarshal.AsSpan(KeyEventList);
        public override ReadOnlySpan<MouseButtonEvent> MouseEvents => CollectionsMarshal.AsSpan(MouseEventList);
        public override ReadOnlySpan<GamepadEvent> GamepadEvents => CollectionsMarshal.AsSpan(GamepadEventList);
        public override ReadOnlySpan<TouchEvent> TouchEvents => CollectionsMarshal.AsSpan(TouchEventList);

        public void SetMousePosition(Vector2 pos)
        {
            MousePosition = pos;
        }

        public void SetWheelDelta(Vector2 delta)
        {
            WheelDelta = delta;
        }

        internal void Clear()
        {
            KeyEventList.Clear();
            MouseEventList.Clear();
            GamepadEventList.Clear();
            TouchEventList.Clear();
            WheelDelta = Vector2.Zero;
        }
    }
}
