using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FreeTypeBindings
{
    public static unsafe class FT
    {
        private const string FreetypeDll = "libfreetype";
        private const CallingConvention CallConvention = CallingConvention.Cdecl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckResult(Error error)
        {
            if (error != Error.Ok)
            {
                ThrowException(error);
            }
        }

        private static void ThrowException(Error error)
            => throw new FreeTypeException(error);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Init_FreeType(out IntPtr alibrary);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Done_FreeType(IntPtr library);

        [DllImport(FreetypeDll, CallingConvention = CallConvention, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern Error FT_New_Face(IntPtr library, string filepathname, int face_index, out Face* aface);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_New_Memory_Face(IntPtr library, IntPtr file_base, int file_size, int face_index, out Face* aface);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Open_Face(IntPtr library, IntPtr args, int face_index, out Face* aface);

        [DllImport(FreetypeDll, CallingConvention = CallConvention, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern Error FT_Attach_File(Face* face, string filepathname);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Attach_Stream(Face* face, IntPtr parameters);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Reference_Face(Face* face);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Done_Face(Face* face);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Select_Size(Face* face, int strike_index);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Request_Size(Face* face, IntPtr req);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Set_Char_Size(Face* face, IntPtr char_width, IntPtr char_height, uint horz_resolution, uint vert_resolution);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Set_Pixel_Sizes(Face* face, uint pixel_width, uint pixel_height);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Load_Glyph(Face* face, uint glyph_index, LoadFlags load_flags);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Load_Char(Face* face, uint char_code, LoadFlags load_flags);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Set_Transform(Face* face, IntPtr matrix, IntPtr delta);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Render_Glyph(GlyphSlot* slot, RenderMode render_mode);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Get_Kerning(Face* face, uint left_glyph, uint right_glyph, KerningMode kern_mode, out FTVector26Dot6 akerning);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Get_Track_Kerning(Face* face, IntPtr point_size, int degree, out IntPtr akerning);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Get_Glyph_Name(Face* face, uint glyph_index, IntPtr buffer, uint buffer_max);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern uint FT_Get_Char_Index(Face* face, uint charcode);


        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Get_Glyph(GlyphSlot* slot, out Glyph* aglyph);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_Copy(Glyph* source, out Glyph target);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_Transform(Glyph* glyph, ref FTMatrix matrix, ref FTVector delta);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Glyph_Get_CBox(Glyph* glyph, GlyphBBoxMode bbox_mode, out BBox acbox);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_To_Bitmap(ref Glyph* the_glyph, RenderMode render_mode, ref FTVector26Dot6 origin, [MarshalAs(UnmanagedType.U1)] bool destroy);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Done_Glyph(Glyph* glyph);


        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Vector_Transform(ref FTVector vec, ref FTMatrix matrix);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Matrix_Multiply(ref FTMatrix a, ref FTMatrix b);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Matrix_Invert(ref FTMatrix matrix);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Vector_Rotate(ref FTVector vec, IntPtr angle);


        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_New(IntPtr library, uint numPoints, int numContours, out Outline outline);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_Copy(ref Outline source, ref Outline target);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Outline_Translate(ref Outline outline, IntPtr xOffset, IntPtr yOffset);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_Get_Bitmap(IntPtr library, ref Outline outline, ref Bitmap bitmap);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_Render(IntPtr library, ref Outline outline, ref RasterParams @params);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_Get_BBox(ref Outline outline, out BBox bbox);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Outline_Get_CBox(ref Outline outline, out BBox cbox);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Outline_Done(IntPtr library, ref Outline outline);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Bitmap_Init(out Bitmap bitmap);


        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Stroker_New(IntPtr library, out IntPtr stroker);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Stroker_Done(IntPtr stroker);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Stroker_Set(IntPtr stroker, int radius, StrokerLineCap line_cap, StrokerLineJoin line_join, IntPtr miter_limit);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_Stroke(ref Glyph* glyph, IntPtr stroker, [MarshalAs(UnmanagedType.U1)] bool destroy);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_Stroke(ref Glyph glyph, IntPtr stroker, [MarshalAs(UnmanagedType.U1)] bool destroy);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Glyph_StrokeBorder(ref Glyph glyph, IntPtr stroker, [MarshalAs(UnmanagedType.U1)] bool inside, [MarshalAs(UnmanagedType.U1)] bool destroy);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Stroker_ParseOutline(IntPtr stroker, ref Outline outline, [MarshalAs(UnmanagedType.U1)] bool opened);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Stroker_GetBorderCounts(IntPtr stroker, StrokerBorder border, out uint num_points, out uint num_contours);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern Error FT_Stroker_GetCounts(IntPtr stroker, out uint num_points, out uint num_contours);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Stroker_ExportBorder(IntPtr stroker, StrokerBorder border, ref Outline outline);

        [DllImport(FreetypeDll, CallingConvention = CallConvention)]
        public static extern void FT_Stroker_Export(IntPtr stroker, ref Outline outline);
    }
}
