#if WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Windows.UI.Core;

namespace MoeGame.Framework.Platform
{
    public partial class UwpGameWindow : Window
    {
        private CoreWindow _coreWindow;

        public override string Title { get; set; }
        public override int Width { get; set; }
        public override int Height { get; set; }
        public override WindowState WindowState { get; set; }
        public override bool IsVisible { get; set; }
        public override bool IsCursorVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override Rectangle Bounds => throw new NotImplementedException();

        internal override IntPtr Handle => throw new NotImplementedException();

        public override event Action Resized;
        public override event Action Closing;
        public override event Action Closed;
        public override event Action GotFocus;
        public override event Action LostFocus;

        public UwpGameWindow(CoreWindow coreWindow)
        {
            _coreWindow = coreWindow;
        }

        public override void ProcessEvents()
        {
            CoreWindow.GetForCurrentThread().Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
        }
    }
}
#endif