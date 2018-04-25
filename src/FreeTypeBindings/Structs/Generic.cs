using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Generic
    {
        public IntPtr data;
        public IntPtr finalizer;
    }
}
