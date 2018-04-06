using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Size
    {
        public unsafe Face* face;
        public Generic generic;
        public SizeMetrics metrics;
        private IntPtr @internal;
    }
}
