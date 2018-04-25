using System;
using System.Runtime.InteropServices;
using FT_Long = System.IntPtr;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GlyphSlot
    {
        public IntPtr library;
        public Face* face;
        public GlyphSlot* next;
        public uint reserved;
        public Generic generic;

        public GlyphMetrics metrics;
        public FT_Long linearHoriAdvance;
        public FT_Long linearVertAdvance;
        public FTVector26Dot6 advance;

        public GlyphFormat format;

        public Bitmap bitmap;
        public int bitmap_left;
        public int bitmap_top;

        public Outline outline;

        public uint num_subglyphs;
        public IntPtr subglyphs;

        public IntPtr control_data;
        public FT_Long control_len;

        public FT_Long lsb_delta;
        public FT_Long rsb_delta;

        public IntPtr other;

        private IntPtr @internal;
    }
}
