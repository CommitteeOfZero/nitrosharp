#if !WINDOWS_UWP
using OpenTK;
using OpenTK.Graphics;
using CommitteeOfZero.Nitro.Foundation.Input;
using System;

namespace CommitteeOfZero.Nitro.Foundation.Platform
{
    public partial class GameWindow : Window
    {
        private NativeWindow _nativeWindow;
        private readonly int _desiredWidth, _desiredHeight;

        public GameWindow() : this("Sample Text", 800, 600, WindowState.Normal)
        {
        }

        public GameWindow(string title, int desiredWidth, int desiredHeight, WindowState state)
        {
            _desiredWidth = desiredWidth;
            _desiredHeight = desiredHeight;
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

        public override System.Numerics.Vector2 ScaleFactor => new System.Numerics.Vector2((float)_nativeWindow.Width / _desiredWidth, (float)_nativeWindow.Height / _desiredHeight);

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
            get { return OtkToNitroWindowState(_nativeWindow.WindowState, _nativeWindow.WindowBorder); }
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
        public override System.Drawing.Rectangle Bounds => OtkToNitroRectangle(_nativeWindow.Bounds);
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

                    var point = new System.Drawing.Point(_nativeWindow.X, _nativeWindow.Y);
                    SetCenteredFullScreenWindow(point);
                    break;
            }

            _nativeWindow.WindowState = NitroToOtkWindowState(state);
        }
    }
}
#endif
