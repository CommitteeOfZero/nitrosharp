using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapGlyph
    {
        public Glyph root;
        public int left;
        public int top;
        public Bitmap bitmap;
    }
}
