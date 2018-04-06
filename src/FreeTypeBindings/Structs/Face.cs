using System;
using System.Runtime.InteropServices;

using FT_Long = System.IntPtr;

namespace FreeTypeBindings
{
    [StructLayout(LayoutKind.Sequential)]
	public unsafe struct Face
	{
		public FT_Long num_faces;
		public FT_Long face_index;

		public FT_Long face_flags;
		public FT_Long style_flags;

		public FT_Long num_glyphs;

		public IntPtr family_name;
        public IntPtr style_name;

		public int num_fixed_sizes;
		public IntPtr available_sizes;

		public int num_charmaps;
		public IntPtr charmaps;

		public Generic generic;

		public BBox bbox;

		public ushort units_per_EM;
		public short ascender;
		public short descender;
		public short height;

		public short max_advance_width;
		public short max_advance_height;

		public short underline_position;
		public short underline_thickness;

		public GlyphSlot* glyph;
		public Size* size;
		public IntPtr charmap;

		private IntPtr driver;
		private IntPtr memory;
		private IntPtr stream;

		private IntPtr sizes_list;
		private Generic autohint;
		private IntPtr extensions;

		private IntPtr @internal;
	}
}
