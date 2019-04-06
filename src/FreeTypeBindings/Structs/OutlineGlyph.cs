using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OutlineGlyph
    {
        public Glyph root;
        public Outline outline;
    }
}
