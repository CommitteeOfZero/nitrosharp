using System;

namespace FreeTypeBindings
{
    public unsafe struct RasterParams
    {
        public Bitmap* target;
        public IntPtr source;
        public int flags;
        public IntPtr gray_spans;
        public IntPtr black_spans;
        public IntPtr bit_test;
        public IntPtr bit_set;
        public IntPtr user;
        public BBox clip_box;
    }
}
