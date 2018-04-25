using System;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Glyph
    {
        public IntPtr library;
        private IntPtr clazz;
        public GlyphFormat format;
        public FTVector advance;
    }
}
