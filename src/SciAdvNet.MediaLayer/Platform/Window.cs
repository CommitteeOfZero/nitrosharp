using System;
using System.Drawing;

namespace SciAdvNet.MediaLayer.Platform
{
    public abstract class Window
    {
        public abstract string Title { get; set; }
        public abstract int Width { get; set; }
        public abstract int Height { get; set; }
        public abstract WindowState WindowState { get; set; }
        public abstract bool Exists { get; }
        public abstract bool IsVisible { get; set; }
        public abstract bool IsCursorVisible { get; set; }
        public abstract Rectangle Bounds { get; }
        internal abstract IntPtr Handle { get; }

        public abstract void ProcessEvents();
        public abstract void Close();

        public abstract event Action Resized;
        public abstract event Action Closing;
        public abstract event Action Closed;
        public abstract event Action GotFocus;
        public abstract event Action LostFocus;
    }
}
