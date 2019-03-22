using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FreeTypeBindings;
using NitroSharp.Primitives;
using System.Collections.Generic;

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
    internal unsafe readonly struct Glyph
    {
        internal readonly Outline Outline;

        public readonly SizeF Size;
        public readonly Size BitmapSize;
        public readonly int BitmapLeft;
        public readonly int BitmapTop;
        public readonly Vector2 Advance;

        internal Glyph(GlyphSlot* slot)
        {
            FT.CheckResult(FT.FT_Outline_New(
                slot->library,
                (uint)slot->outline.n_points,
                slot->outline.n_contours,
                out Outline outlineCopy));

            FT.CheckResult(FT.FT_Outline_Copy(ref slot->outline, ref outlineCopy));
            Outline = outlineCopy;

            GlyphMetrics* metrics = &slot->metrics;
            Size = new SizeF((long)metrics->width / 64.0f,
                             (long)metrics->height / 64.0f);
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
            Outline outline = Outline;
            FT.FT_Outline_Translate(
                ref outline,
                (IntPtr)(-BitmapLeft * 64),
                (IntPtr)((h - BitmapTop) * 64));

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
                    FT.FT_Outline_Get_Bitmap(face.Library, ref outline, ref bmp));
            }
        }
    }

    internal readonly struct GlyphCacheKey
    {
        public readonly int FontSize;
        public readonly char Character;

        public GlyphCacheKey(char character, int fontSize)
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
        private int _currentSize;
        private VerticalMetrics _currentSizeMetrics;
        private readonly Dictionary<GlyphCacheKey, Glyph> _glyphCache;

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
        }

        internal IntPtr Library => _ftInstance;

        public string FontFamily { get; }
        public FontStyle Style { get; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetSize(int ptSize)
        {
            FT.CheckResult(
                FT.FT_Set_Char_Size(_face,
                (IntPtr)Fixed26Dot6.FromInt32(0).Value,
                (IntPtr)Fixed26Dot6.FromInt32(ptSize).Value,
                72, 72));

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

        public Glyph GetGlyph(char c, int ptFontSize)
        {
            var key = new GlyphCacheKey(c, ptFontSize);
            if (!_glyphCache.TryGetValue(key, out Glyph glyph))
            {
                glyph = LoadGlyph(c, ptFontSize);
                _glyphCache.Add(key, glyph);
            }

            return glyph;
        }

        private Glyph LoadGlyph(char c, int ptSize)
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
                Outline outline = g.Outline;
                FT.CheckResult(FT.FT_Outline_Done(_ftInstance, ref outline));
            }

            _glyphCache.Clear();
            FT.FT_Done_Face(_face);
            _face = null;
        }

        ~FontFace()
        {
            Destroy();
        }
    }
}
