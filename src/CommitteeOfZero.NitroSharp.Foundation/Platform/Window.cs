using System;
using System.Drawing;
using System.Numerics;

namespace CommitteeOfZero.NitroSharp.Foundation.Platform
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
        public abstract Vector2 ScaleFactor { get; }
        internal abstract IntPtr Handle { get; }

        public abstract void ProcessEvents();
        public abstract void Close();

        public abstract event EventHandler Resized;
        public abstract event EventHandler Closing;
        public abstract event EventHandler Closed;
        public abstract event EventHandler GotFocus;
        public abstract event EventHandler LostFocus;
    }
}
