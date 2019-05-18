using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FreeTypeBindings;
using System.Collections.Generic;
using Veldrid;

using FTGlyph = FreeTypeBindings.Glyph;
using Size = NitroSharp.Primitives.Size;

namespace NitroSharp.Text
{
    internal enum FontStyle
    {
        Regular = 0,
        Italic,
        Bold
    }

    internal readonly struct VerticalMetrics
    {
        public VerticalMetrics(float ascender, float descender, float lineGap)
            => (Ascender, Descender, LineHeight) = (ascender, descender, lineGap);

        /// <summary>
        /// The highest point that any glyph in the font extends to above
        /// the baseline. Typically positive.
        /// </summary>
        public readonly float Ascender;
        /// <summary>
        /// The lowest point that any glyph in the font extends to below
        /// the baseline. Typically negative.
        /// </summary>
        public readonly float Descender;
        /// <summary>
        /// The distance between two consecutive baselines.
        /// Calculated as Ascender - Descender + LineGap
        /// </summary>
        public readonly float LineHeight;

        public float LineGap => LineHeight - Ascender + Descender;
    }

    //Glyph metrics:
    //--------------
    //
    //                      xMin                     xMax
    //                       |                         |
    //                       |<-------- width -------->|
    //                       |                         |
    //             |         +-------------------------+----------------- yMax
    //             |         |    ggggggggg   ggggg    |     ^        ^
    //             |         |   g:::::::::ggg::::g    |     |        |
    //             |         |  g:::::::::::::::::g    |     |        |
    //             |         | g::::::ggggg::::::gg    |     |        |
    //             |         | g:::::g     g:::::g     |     |        |
    //    offsetX -|-------->| g:::::g     g:::::g     |  offsetY     |
    //             |         | g:::::g     g:::::g     |     |        |
    //             |         | g::::::g    g:::::g     |     |        |
    //             |         | g:::::::ggggg:::::g     |     |        |
    //             |         |  g::::::::::::::::g     |     |      height
    //             |         |   gg::::::::::::::g     |     |        |
    // baseline ---*---------|---- gggggggg::::::g-----*--------      |
    //           / |         |             g:::::g     |              |
    //    origin   |         | gggggg      g:::::g     |              |
    //             |         | g:::::gg   gg:::::g     |              |
    //             |         |  g::::::ggg:::::::g     |              |
    //             |         |   gg:::::::::::::g      |              |
    //             |         |     ggg::::::ggg        |              |
    //             |         |         gggggg          |              v
    //             |         +-------------------------+----------------- yMin
    //             |                                   |
    //             |------------- advanceX ----------->|
    //
    internal unsafe struct Glyph
    {
        internal OutlineGlyph* FTGlyph;

        public readonly Size BitmapSize;
        public readonly int BitmapLeft;
        public readonly int BitmapTop;
        public readonly Vector2 Advance;

        internal Glyph(GlyphSlot* slot)
        {
            FT.FT_Get_Glyph(slot, out FTGlyph* glyphPtr);
            FTGlyph = (OutlineGlyph*)glyphPtr;
            Advance = new Vector2(slot->advance.X.ToSingle(),
                                  slot->advance.Y.ToSingle());
            BitmapSize = new Size((uint)slot->bitmap.width,
                                  (uint)slot->bitmap.rows);
            (BitmapLeft, BitmapTop) = (slot->bitmap_left, slot->bitmap_top);
        }

        public void Rasterize(FontFace face, Span<byte> buffer)
        {
            buffer.Clear();

            (int w, int h) = ((int)BitmapSize.Width, (int)BitmapSize.Height);
            ref Outline outline = ref FTGlyph->outline;
            FT.FT_Outline_Translate(
                ref outline,
                (IntPtr)(-BitmapLeft * 64),
                (IntPtr)((h - BitmapTop) * 64)
            );

            //FT.FT_Glyph_Get_CBox((FTGlyph*)FTGlyph, GlyphBBoxMode.Pixels, out BBox bbox);
            //Debug.Assert(bbox.Left == BitmapLeft);
            //Debug.Assert(bbox.Top == BitmapTop);

            fixed (byte* ptr = &buffer[0])
            {
                var bmp = new Bitmap
                {
                    buffer = new IntPtr(ptr),
                    width = w,
                    pitch = w,
                    rows = h,
                    pixel_mode = PixelMode.Gray,
                    num_grays = 256
                };

                FT.CheckResult(
                    FT.FT_Outline_Get_Bitmap(face.Library, ref outline, ref bmp)
                );

                //Span<byte> sdf = stackalloc byte[w * h];
                //Sdf.make_distance_mapb(buffer, w, h, sdf);
                //sdf.CopyTo(buffer);
            }
        }

        //public void RasterizeWithOutline(FontFace face, Span<byte> buffer, out Size size)
        //{
        //    buffer.Clear();

        //    (int w, int h) = ((int)BitmapSize.Width, (int)BitmapSize.Height);
        //    ref Outline outline = ref FTGlyph->outline;

        //    var radius = Fixed16Dot16.FromInt32(2);
        //    IntPtr stroker = face.Stroker;
        //    FT.FT_Stroker_Set(
        //        stroker,
        //        64 * 2,
        //        StrokerLineCap.Round,
        //        StrokerLineJoin.Round,
        //        miter_limit: IntPtr.Zero
        //    );

        //    var glyph = (FTGlyph*)FTGlyph;
        //    FT.FT_Glyph_Stroke(ref glyph, face.Stroker, destroy: true);
        //    var origin = new FTVector26Dot6(Fixed26Dot6.FromInt32(0), Fixed26Dot6.FromInt32(0));
        //    FT.CheckResult(FT.FT_Glyph_To_Bitmap(ref glyph, RenderMode.Normal, ref origin, destroy: true));

        //    BitmapGlyph* bmp = (BitmapGlyph*)glyph;
        //    size = new Size((uint)bmp->bitmap.width, (uint)bmp->bitmap.rows);

        //    var span = new Span<byte>(bmp->bitmap.buffer.ToPointer(), (int)(size.Width * size.Height));
        //}

        unsafe struct Pixel
        {
            public fixed byte Channels[4];
        }

        public ReadOnlySpan<RgbaByte> RasterizeOutlineHack(FontFace face, out Size size, out Vector2 bitmapOffset)
        {
            var glyph = (FTGlyph*)FTGlyph;
            IntPtr stroker = face.Stroker;

            var buffer = new Pixel[100 * 100];
            int w = 0, h = 0;
            BBox bbox = default;
            int ch = 3;
            for (int i = 4; i > 0; i--)
            {
                FT.FT_Stroker_Set(
                    stroker,
                    64 * i,
                    StrokerLineCap.Round,
                    StrokerLineJoin.Round,
                    miter_limit: IntPtr.Zero
                );

                FTGlyph* stroked = (FTGlyph*)FTGlyph;
                FT.CheckResult(FT.FT_Glyph_Stroke(ref stroked, stroker, destroy: false));
                FTVector26Dot6 origin = default;
                FT.CheckResult(FT.FT_Glyph_To_Bitmap(ref stroked, RenderMode.Normal, ref origin, destroy: false));
                BitmapGlyph* bmp = (BitmapGlyph*)stroked;
                ref readonly Bitmap bitmap = ref bmp->bitmap;
                var srcBuffer = new ReadOnlySpan<byte>(bitmap.buffer.ToPointer(), bitmap.width * bitmap.rows);
                if (i == 4)
                {
                    w = bmp->bitmap.width;
                    h = bmp->bitmap.rows;
                    FT.FT_Glyph_Get_CBox(glyph, GlyphBBoxMode.Pixels, out bbox);
                }

                int dstStartX = (w - bitmap.width) / 2;
                int dstStartY = (h - bitmap.rows) / 2;
                int thisH = bitmap.rows;
                int thisW = bitmap.width;
                for (int srcY = 0; srcY < thisH; srcY++)
                {
                    for (int srcX = 0; srcX < thisW; srcX++)
                    {
                        int index = w * (dstStartY + srcY) + dstStartX + srcX;
                        buffer[index].Channels[ch] = srcBuffer[srcY * thisW + srcX];
                    }
                }

                ch--;
            }

            size = new Size((uint)w, (uint)h);
            bitmapOffset = new Vector2(bbox.Left, bbox.Top);
            return MemoryMarshal.Cast<Pixel, RgbaByte>(buffer.AsSpan(0, w * h));
        }

        public void RasterizeOutline(FontFace face, Span<byte> buffer, out Size size, out Vector2 bitmapOffset)
        {
            buffer.Clear();

            (int w, int h) = ((int)BitmapSize.Width, (int)BitmapSize.Height);
            ref Outline outline = ref FTGlyph->outline;

            var radius = Fixed16Dot16.FromInt32(2);
            IntPtr stroker = face.Stroker;
            FT.FT_Stroker_Set(
                stroker,
                64 * 4,
                StrokerLineCap.Round,
                StrokerLineJoin.Round,
                miter_limit: IntPtr.Zero
            );

            var glyph = (FTGlyph*)FTGlyph;
            FT.FT_Glyph_Stroke(ref glyph, face.Stroker, destroy: true);
            FT.FT_Glyph_Get_CBox(glyph, GlyphBBoxMode.Pixels, out BBox bbox);
            //var origin = new FTVector26Dot6(Fixed26Dot6.FromInt32(-bbox.Left), Fixed26Dot6.FromInt32(-bbox.Top));
            var origin = new FTVector26Dot6(Fixed26Dot6.FromInt32(0), Fixed26Dot6.FromInt32(0));
            FT.CheckResult(FT.FT_Glyph_To_Bitmap(ref glyph, RenderMode.Normal, ref origin, destroy: true));

            BitmapGlyph* bmp = (BitmapGlyph*)glyph;
            size = new Size((uint)bmp->bitmap.width, (uint)bmp->bitmap.rows);
            var span = new Span<byte>(bmp->bitmap.buffer.ToPointer(), (int)(size.Width * size.Height));
            span.CopyTo(buffer);

            bitmapOffset = new Vector2(bbox.Left, bbox.Top);

            //FT.CheckResult(
            //    FT.FT_Stroker_ParseOutline(stroker, ref outline, opened: false)
            //);
            //FT.CheckResult(
            //    FT.FT_Stroker_GetCounts(
            //        stroker,
            //        out uint numPoints,
            //        out uint numContours
            //    )
            //);
            //FT.CheckResult(
            //    FT.FT_Outline_New(face.Library, numPoints, (int)numContours, out Outline newOutline)
            //);

            //newOutline.n_contours = 0;
            //newOutline.n_points = 0;
            //FT.FT_Stroker_Export(stroker, ref newOutline);

            //FT.FT_Outline_Get_BBox(ref newOutline, out BBox outlineBox);
            //size = new Size(
            //    width: (uint)(outlineBox.Right - outlineBox.Left),
            //    height: (uint)(outlineBox.Bottom - outlineBox.Top)
            //);

            //FT.FT_Outline_Translate(
            //    ref newOutline,
            //    (IntPtr)(-1 * 64),
            //    (IntPtr)(1 * 64)
            //);
            //FT.FT_Outline_Translate(
            //    ref outline,
            //    (IntPtr)(-BitmapLeft * 64),
            //    (IntPtr)((h - BitmapTop) * 64)
            //);
            //outline = newOutline;

            //fixed (byte* ptr = &buffer[0])
            //{
            //    var bmp = new Bitmap
            //    {
            //        buffer = new IntPtr(ptr),
            //        width = w,
            //        pitch = w,
            //        rows = h,
            //        pixel_mode = PixelMode.Gray,
            //        num_grays = 256
            //    };

            //    FT.CheckResult(
            //        FT.FT_Outline_Get_Bitmap(face.Library, ref outline, ref bmp)
            //    );
            //    FT.CheckResult(
            //       FT.FT_Outline_Get_Bitmap(face.Library, ref newOutline, ref bmp)
            //   );
            //}
        }
    }

    internal readonly struct GlyphCacheKey
    {
        public readonly float FontSize;
        public readonly char Character;

        public GlyphCacheKey(char character, float fontSize)
        {
            Character = character;
            FontSize = fontSize;
        }

        public override int GetHashCode()
            => HashCode.Combine((int)Character, FontSize);
    }

    internal sealed unsafe class FontFace : IDisposable
    {
        private readonly IntPtr _ftInstance;

        private Face* _face;
        private float _currentSize;
        private VerticalMetrics _currentSizeMetrics;
        private readonly Dictionary<GlyphCacheKey, Glyph> _glyphCache;

        private IntPtr _stroker;

        public FontFace(IntPtr ftInstance, Face* face)
        {
            _ftInstance = ftInstance;
            _face = face;
            FontFamily = Marshal.PtrToStringAnsi(face->family_name);
            string style = Marshal.PtrToStringAnsi(face->style_name);
            switch (style)
            {
                case "Italic":
                    Style = FontStyle.Italic;
                    break;
                case "Bold":
                    Style = FontStyle.Bold;
                    break;
                case "Regular":
                default:
                    Style = FontStyle.Regular;
                    break;
            }

            _glyphCache = new Dictionary<GlyphCacheKey, Glyph>(1024);
            FT.FT_Stroker_New(ftInstance, out _stroker);
        }

        internal IntPtr Library => _ftInstance;
        internal IntPtr Stroker => _stroker;

        public string FontFamily { get; }
        public FontStyle Style { get; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetSize(float ptSize)
        {
            FT.CheckResult(
                FT.FT_Set_Char_Size(_face,
                    (IntPtr)Fixed26Dot6.FromSingle(0).Value,
                    (IntPtr)Fixed26Dot6.FromSingle(ptSize).Value,
                    72, 72)
            );

            _currentSize = ptSize;
            SizeMetrics metrics = _face->size->metrics;
            float ascender = Fixed26Dot6.FromRawValue((int)metrics.ascender).ToSingle();
            float descender = Fixed26Dot6.FromRawValue((int)metrics.descender).ToSingle();
            float height = Fixed26Dot6.FromRawValue((int)metrics.height).ToSingle();
            _currentSizeMetrics = new VerticalMetrics(ascender, descender, height);
        }

        public VerticalMetrics GetVerticalMetrics(int ptSize)
        {
            if (ptSize != _currentSize)
            {
                SetSize(ptSize);
            }

            return _currentSizeMetrics;
        }

        public Glyph GetGlyph(char c, float ptFontSize)
        {
            var key = new GlyphCacheKey(c, ptFontSize);
            if (!_glyphCache.TryGetValue(key, out Glyph glyph))
            {
                glyph = LoadGlyph(c, ptFontSize);
                _glyphCache.Add(key, glyph);
            }

            return glyph;
        }

        private Glyph LoadGlyph(char c, float ptSize)
        {
            if (ptSize != _currentSize)
            {
                SetSize(ptSize);
            }

            Face* face = _face;
            FT.CheckResult(FT.FT_Load_Char(face, c, LoadFlags.Default));
            return new Glyph(face->glyph);
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            foreach (Glyph g in _glyphCache.Values)
            {
                FT.FT_Done_Glyph((FTGlyph*)g.FTGlyph);
            }

            _glyphCache.Clear();
            FT.FT_Stroker_Done(_stroker);
            FT.FT_Done_Face(_face);
            _face = null;
        }

        ~FontFace()
        {
            Destroy();
        }
    }
}
