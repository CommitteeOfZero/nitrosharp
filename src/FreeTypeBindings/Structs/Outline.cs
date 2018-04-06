using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Outline
    {
        public short n_contours;
        public short n_points;

        public IntPtr points;
        public IntPtr tags;
        public IntPtr contours;

        public OutlineFlags flags;
    }
}
