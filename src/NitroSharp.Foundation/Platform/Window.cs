using System;
using System.Drawing;

namespace NitroSharp.Foundation.Platform
{
    public abstract class Window
    {
        public abstract string Title { get; set; }
        public abstract Size Size { get; set; }
        public abstract WindowState WindowState { get; set; }
        public abstract bool Exists { get; }
        public abstract bool IsVisible { get; set; }
        public abstract bool IsCursorVisible { get; set; }
        public abstract Rectangle Bounds { get; }
        internal abstract IntPtr Handle { get; }

        public abstract InputSnapshot GetInputSnapshot();
        public abstract void ToggleBorderlessFullscreen();
        public abstract void Close();

        public abstract event EventHandler Resized;
        public abstract event EventHandler Closing;
        public abstract event EventHandler Closed;
        public abstract event EventHandler GotFocus;
        public abstract event EventHandler LostFocus;
    }
}
