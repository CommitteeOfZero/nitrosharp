using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Bitmap
    {
        public int rows;
        public int width;
        public int pitch;
        public IntPtr buffer;
        public short num_grays;
        public PixelMode pixel_mode;
        public byte palette_mode;
        public IntPtr palette;
    }
}
