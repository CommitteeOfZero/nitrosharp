// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;
using System.Runtime.InteropServices;

#if NETSTANDARD1_4
using OtkPoint = OpenTK.Point;
using OtkSize = OpenTK.Size;
#else
using OtkPoint = System.Drawing.Point;
using OtkSize = System.Drawing.Size;
#endif

namespace NitroSharp.Foundation.Platform
{
    public abstract class GameWindowBase : Window
    {
        protected NativeWindow _nativeWindow;
        private System.Drawing.Size _previousSize;
        private System.Drawing.Point _previousPosition;
        private bool[] _mouseDown = new bool[13];
        protected InputSnapshot _currentSnapshot = new InputSnapshot();

        public override event EventHandler Resized;
        public override event EventHandler Closing;
        public override event EventHandler Closed;
        public override event EventHandler GotFocus;
        public override event EventHandler LostFocus;

        protected GameWindowBase() : this("Sample Text", 800, 600, WindowState.Normal)
        {
        }

        protected GameWindowBase(string title, int desiredWidth, int desiredHeight, WindowState state)
        {
            Create(title, desiredWidth, desiredHeight, state);
        }

        public override string Title
        {
            get => _nativeWindow.Title;
            set => _nativeWindow.Title = value;
        }

        public override System.Drawing.Size Size
        {
            get => new System.Drawing.Size(_nativeWindow.Width, _nativeWindow.Height);
            set => _nativeWindow.Size = new OtkSize(value.Width, value.Height);
        }

        public override WindowState WindowState
        {
            get => OtkToNitroWindowState(_nativeWindow.WindowState, _nativeWindow.WindowBorder);
            set => SetWindowState(value);
        }

        public override bool Exists => _nativeWindow.Exists;
        public override bool IsVisible
        {
            get => _nativeWindow.Visible;
            set => _nativeWindow.Visible = value;
        }

        public override bool IsCursorVisible
        {
            get => _nativeWindow.CursorVisible;
            set => _nativeWindow.CursorVisible = value;
        }

#if NETSTANDARD1_4
        public override System.Drawing.Rectangle Bounds => OtkToNitroRectangle(_nativeWindow.Bounds);
#else
        public override System.Drawing.Rectangle Bounds => _nativeWindow.Bounds;
#endif

        public override IntPtr Handle => _nativeWindow.WindowInfo.Handle;

        protected virtual void Create(string title, int desiredWidth, int desiredHeight, WindowState state)
        {
            var graphicsMode = new GraphicsMode(32, 24, 0, 8);
            _nativeWindow = new NativeWindow(desiredWidth, desiredHeight, title, GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);

            _nativeWindow.Resize += OnWindowResized;
            _nativeWindow.Closing += OnWindowClosing;
            _nativeWindow.Closed += OnWindowClosed;
            _nativeWindow.FocusedChanged += OnWindowFocusedChanged;

            _nativeWindow.KeyDown += OnKeyDown;
            _nativeWindow.KeyUp += OnKeyUp;
            _nativeWindow.MouseDown += OnMouseDown;
            _nativeWindow.MouseUp += OnMouseUp;

            WindowState = state;
            IsVisible = true;
        }

        protected abstract InputSnapshot GetAvailableSnapshot();

        public override InputSnapshot GetInputSnapshot()
        {
            var snapshot = GetAvailableSnapshot();
            if (_nativeWindow.Exists)
            {
                MouseState cursorState = Mouse.GetCursorState();
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var windowPoint = _nativeWindow.PointToClient(new OtkPoint(cursorState.X, cursorState.Y));
                    snapshot.MousePosition = new System.Numerics.Vector2(
                        windowPoint.X,
                        windowPoint.Y); // ScaleFactor;
                }
                else
                {
                    snapshot.MousePosition = new System.Numerics.Vector2(
                        cursorState.X,
                        cursorState.Y); // ScaleFactor;
                }
            }
            _mouseDown.CopyTo(snapshot.MouseDown, 0);
            return snapshot;
        }

        public override void ToggleBorderlessFullscreen()
        {
            WindowState = WindowState != WindowState.BorderlessFullScreen ? WindowState.BorderlessFullScreen : WindowState.Normal;
        }

        public override void Close()
        {
            _nativeWindow.Close();
        }

        private void OnWindowFocusedChanged(object sender, EventArgs e)
        {
            if (_nativeWindow.Focused)
            {
                GotFocus?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                LostFocus?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            _currentSnapshot.KeyEventsList.Add(new KeyEvent((Key)e.Key, true, ConvertModifiers(e.Modifiers)));
        }

        private void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            _currentSnapshot.KeyEventsList.Add(new KeyEvent((Key)e.Key, false, ConvertModifiers(e.Modifiers)));
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown[(int)e.Button] = true;
            _currentSnapshot.MouseEventsList.Add(new MouseEvent((MouseButton)e.Button, true));
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDown[(int)e.Button] = false;
            _currentSnapshot.MouseEventsList.Add(new MouseEvent((MouseButton)e.Button, false));
        }

        private ModifierKeys ConvertModifiers(KeyModifiers modifiers)
        {
            ModifierKeys modifierKeys = ModifierKeys.None;
            if ((modifiers & KeyModifiers.Alt) == KeyModifiers.Alt)
            {
                modifierKeys |= ModifierKeys.Alt;
            }
            if ((modifiers & KeyModifiers.Control) == KeyModifiers.Control)
            {
                modifierKeys |= ModifierKeys.Control;
            }
            if ((modifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            {
                modifierKeys |= ModifierKeys.Shift;
            }

            return modifierKeys;
        }

        private void OnWindowResized(object sender, EventArgs e)
        {
            Resized?.Invoke(this, e);
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Closed?.Invoke(this, e);
        }

#if NETSTANDARD1_4
        private static System.Drawing.Rectangle OtkToNitroRectangle(OpenTK.Rectangle rect)
        {
            return new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
#endif

        private static WindowState OtkToNitroWindowState(OpenTK.WindowState state, WindowBorder border)
        {
            switch (state)
            {
                case OpenTK.WindowState.Minimized:
                    return WindowState.Minimized;
                case OpenTK.WindowState.Maximized:
                    return WindowState.Maximized;
                case OpenTK.WindowState.Fullscreen:
                    return WindowState.FullScreen;


                case OpenTK.WindowState.Normal:
                default:
                    return border == WindowBorder.Hidden ? WindowState.BorderlessFullScreen : WindowState.Normal;
            }
        }

        private OpenTK.WindowState NitroToOtkWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.FullScreen:
                    return OpenTK.WindowState.Fullscreen;
                case WindowState.Maximized:
                    return OpenTK.WindowState.Maximized;
                case WindowState.Minimized:
                    return OpenTK.WindowState.Minimized;
                case WindowState.Normal:
                default:
                    return OpenTK.WindowState.Normal;
            }
        }

        private void SetCenteredFullScreenWindow(System.Drawing.Point position)
        {
            int x = position.X;
            int actualX = 0;
            System.Drawing.Size size = default(System.Drawing.Size);
            DisplayIndex index = DisplayIndex.Default;
            while (x >= 0)
            {
                var display = DisplayDevice.GetDisplay(index);
                x -= display.Width;
                if (x > 0)
                {
                    actualX += display.Width;
                }
                else
                {
                    size = new System.Drawing.Size(display.Width, display.Height);
                }

                index += 1;
            }

            if (size == default(System.Drawing.Size))
            {
                throw new InvalidOperationException("SetCenteredFullScreen failed. Couldn't determine size.");
            }

            var bounds = _nativeWindow.Bounds;
            bounds.X = actualX;
            bounds.Y = 0;
            bounds.Width = size.Width;
            bounds.Height = size.Height;
            _nativeWindow.Bounds = bounds;
        }

        private void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    _nativeWindow.WindowBorder = WindowBorder.Fixed;
                    _nativeWindow.WindowState = OpenTK.WindowState.Normal;
                    if (_previousSize != default(System.Drawing.Size))
                    {
                        _nativeWindow.ClientSize = new OtkSize(_previousSize.Width, _previousSize.Height);
                    }
                    if (_previousPosition != default(System.Drawing.Point))
                    {
                        _nativeWindow.X = _previousPosition.X;
                        _nativeWindow.Y = _previousPosition.Y;
                    }
                    break;

                case WindowState.Minimized:
                case WindowState.Maximized:
                    _nativeWindow.WindowBorder = WindowBorder.Fixed;
                    break;

                case WindowState.BorderlessFullScreen:
                    _nativeWindow.WindowBorder = WindowBorder.Hidden;
                    if (_nativeWindow.WindowState != OpenTK.WindowState.Normal)
                    {
                        _nativeWindow.WindowState = OpenTK.WindowState.Normal;
                    }

#if NETSTANDARD1_4
                    _previousSize = new System.Drawing.Size(_nativeWindow.Size.Width, _nativeWindow.Size.Height);
#else
                    _previousSize = _nativeWindow.Size;
#endif

                    _previousPosition = new System.Drawing.Point(_nativeWindow.X, _nativeWindow.Y);
                    SetCenteredFullScreenWindow(_previousPosition);
                    break;
            }

            _nativeWindow.WindowState = NitroToOtkWindowState(state);
        }
    }
}
