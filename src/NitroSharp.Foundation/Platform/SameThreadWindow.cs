// Based on code from the Veldrid open source library
// https://github.com/mellinoe/veldrid

namespace NitroSharp.Foundation.Platform
{
    public class SameThreadWindow : GameWindowBase
    {
        public SameThreadWindow()
        {
        }

        public SameThreadWindow(string title, int desiredWidth, int desiredHeight, WindowState state)
            : base(title, desiredWidth, desiredHeight, state)
        {
        }

        protected override InputSnapshot GetAvailableSnapshot()
        {
            _currentSnapshot.Clear();
            _nativeWindow.ProcessEvents();
            return _currentSnapshot;
        }
    }
}
