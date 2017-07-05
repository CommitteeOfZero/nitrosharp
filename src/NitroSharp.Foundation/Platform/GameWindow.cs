#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Graphics;
using NitroSharp.Foundation.Input;
using System;

namespace NitroSharp.Foundation.Platform
{
    public partial class GameWindow : Window
    {
        private NativeWindow _nativeWindow;
        private System.Drawing.Size _previousSize;
        private System.Drawing.Point _previousPosition;

        public GameWindow() : this("Sample Text", 800, 600, WindowState.Normal)
        {
        }

        public GameWindow(string title, int desiredWidth, int desiredHeight, WindowState state)
        {
            DesiredSize = new System.Drawing.Size(desiredWidth, desiredHeight);
            var graphicsMode = new GraphicsMode(32, 24, 0, 8);
            _nativeWindow = new NativeWindow(desiredWidth, desiredHeight, title, GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);

            _nativeWindow.Resize += OnWindowResized;
            _nativeWindow.Closing += OnWindowClosing;
            _nativeWindow.Closed += OnWindowClosed;
            _nativeWindow.FocusedChanged += OnWindowFocusedChanged;

            SubsribeToInputEvents();

            WindowState = state;
            IsVisible = true;
        }

        public override System.Drawing.Size DesiredSize { get; }
        public override System.Numerics.Vector2 ScaleFactor
        {
            get
            {
                return new System.Numerics.Vector2((float)_nativeWindow.Width / DesiredSize.Width, (float)_nativeWindow.Height / DesiredSize.Height);
            }
        }

        public override string Title
        {
            get =>_nativeWindow.Title;
            set => _nativeWindow.Title = value;
        }

        public override int Width
        {
            get => _nativeWindow.Width;
            set => _nativeWindow.Width = value;
        }

        public override int Height
        {
            get => _nativeWindow.Height;
            set => _nativeWindow.Height = value;
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

        public override event EventHandler Resized;
        public override event EventHandler Closing;
        public override event EventHandler Closed;
        public override event EventHandler GotFocus;
        public override event EventHandler LostFocus;

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

#if NETSTANDARD1_4
        public override System.Drawing.Rectangle Bounds => OtkToNitroRectangle(_nativeWindow.Bounds);
#else
        public override System.Drawing.Rectangle Bounds => _nativeWindow.Bounds;
#endif

        internal override IntPtr Handle => _nativeWindow.WindowInfo.Handle;

        public override void ToggleBorderlessFullscreen()
        {
            WindowState = WindowState != WindowState.BorderlessFullScreen ? WindowState.BorderlessFullScreen : WindowState.Normal;
        }

        public override void ProcessEvents()
        {
            Mouse.NewlyPressedButtons.Clear();
            Keyboard.NewlyPressedKeys.Clear();
            _nativeWindow.ProcessEvents();
        }

        public override void Close()
        {
            _nativeWindow.Close();
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
#if NETSTANDARD1_4
                        _nativeWindow.ClientSize = new OpenTK.Size(_previousSize.Width, _previousSize.Height);
#else

                        _nativeWindow.ClientSize = new System.Drawing.Size(_previousSize.Width, _previousSize.Height);
#endif
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
#endif
