#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Graphics;
using CommitteeOfZero.Nitro.Foundation.Input;
using System;
using System.Drawing;

namespace CommitteeOfZero.Nitro.Foundation.Platform
{
    public partial class GameWindow : Window
    {
        private NativeWindow _nativeWindow;

        public GameWindow() : this("Sample Text", 800, 600, WindowState.Normal)
        {
        }

        public GameWindow(string title, int width, int height, WindowState state)
        {
            var graphicsMode = new GraphicsMode(32, 24, 0, 8);
            _nativeWindow = new NativeWindow(width, height, title, GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);

            _nativeWindow.Resize += OnWindowResized;
            _nativeWindow.Closing += OnWindowClosing;
            _nativeWindow.Closed += OnWindowClosed;
            _nativeWindow.FocusedChanged += OnWindowFocusedChanged;

            SubsribeToInputEvents();

            WindowState = state;
            IsVisible = true;
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

        public override event EventHandler Resized;
        public override event EventHandler Closing;
        public override event EventHandler Closed;
        public override event EventHandler GotFocus;
        public override event EventHandler LostFocus;

        public override string Title
        {
            get { return _nativeWindow.Title; }
            set { _nativeWindow.Title = value; }
        }

        public override int Width
        {
            get { return _nativeWindow.Width; }
            set { _nativeWindow.Width = value; }
        }

        public override int Height
        {
            get { return _nativeWindow.Height; }
            set { _nativeWindow.Height = value; }
        }

        public override WindowState WindowState
        {
            get { return OtkToMoeWindowState(_nativeWindow.WindowState); }
            set { SetWindowState(value); }
        }

        public override bool Exists => _nativeWindow.Exists;
        public override bool IsVisible
        {
            get { return _nativeWindow.Visible; }
            set { _nativeWindow.Visible = value; }
        }

        public override bool IsCursorVisible
        {
            get { return _nativeWindow.CursorVisible; }
            set { _nativeWindow.CursorVisible = value; }
        }

#if NETSTANDARD1_4
        public override System.Drawing.Rectangle Bounds => OtkToMoeRectangle(_nativeWindow.Bounds);
#else
        public override System.Drawing.Rectangle Bounds => _nativeWindow.Bounds;
#endif

        internal override IntPtr Handle => _nativeWindow.WindowInfo.Handle;

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
        private static System.Drawing.Rectangle OtkToMoeRectangle(OpenTK.Rectangle rect)
        {
            return new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
#endif

        private static WindowState OtkToMoeWindowState(OpenTK.WindowState otkWindowState)
        {
            switch (otkWindowState)
            {
                case OpenTK.WindowState.Minimized:
                    return WindowState.Minimized;
                case OpenTK.WindowState.Maximized:
                    return WindowState.Maximized;
                case OpenTK.WindowState.Fullscreen:
                    return WindowState.FullScreen;

                case OpenTK.WindowState.Normal:
                default:
                    return WindowState.Normal;
            }
        }

        private static OpenTK.WindowState MoeToOtkWindowState(WindowState mlWindowState)
        {
            switch (mlWindowState)
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

        private void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                case WindowState.Minimized:
                case WindowState.Maximized:
                    _nativeWindow.WindowBorder = WindowBorder.Resizable;
                    break;
            }

            _nativeWindow.WindowState = MoeToOtkWindowState(state);
        }
    }
}
#endif
