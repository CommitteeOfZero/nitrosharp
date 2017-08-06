using System;
using System.Runtime.InteropServices;

namespace NitroSharp.Foundation.Platform
{
    public abstract class MessageBox
    {
        // TODO: implement for all platforms.
        private static readonly MessageBox s_impl = new WindowsMessageBox();

        public static void Show(IntPtr windowHandle, string text, bool isError = false)
        {
            s_impl.PlatformShow(windowHandle, text, isError);
        }

        protected abstract void PlatformShow(IntPtr windowHandle, string text, bool isError);
    }

    public class WindowsMessageBox : MessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint options);

        private const uint MB_ICONERROR = 0x00000010;

        protected override void PlatformShow(IntPtr windowHandle, string text, bool isError)
        {
            MessageBox(windowHandle, text, string.Empty, isError ? MB_ICONERROR : 0);
        }
    }
}
