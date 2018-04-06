using System.Runtime.InteropServices;

using FT_Long = System.IntPtr;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SizeMetrics
    {
        public ushort x_ppem;
        public ushort y_ppem;

        public FT_Long x_scale;
        public FT_Long y_scale;
        public FT_Long ascender;
        public FT_Long descender;
        public FT_Long height;
        public FT_Long max_advance;
    }
}
