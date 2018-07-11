using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeBindings;
using NitroSharp.Primitives;
using NitroSharp.Utilities;

namespace NitroSharp.Text
{
    internal sealed class FontFace : IDisposable
    {
        private unsafe Face* _face;

        private ArrayBuilder<GlyphInfo> _cachedGlyphs;
        private readonly Dictionary<char, uint> _cacheEntryIndices;

        public unsafe FontFace(Face* face)
        {
            _face = face;
            _cachedGlyphs = new ArrayBuilder<GlyphInfo>(256);
            _cacheEntryIndices = new Dictionary<char, uint>(capacity: 256);

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
        }

        public string FontFamily { get; }
        public FontStyle Style { get; }
        public float CurrentSize { get; private set; }
        public FontMetrics ScaledMetrics { get; private set; }

        public void SetSize(float ptSize)
        {
            if (ptSize != CurrentSize)
            {
                unsafe
                {
                    FT.CheckResult(
                        FT.FT_Set_Char_Size(_face,
                        (IntPtr)Fixed26Dot6.FromInt32(0).Value,
                        (IntPtr)Fixed26Dot6.FromSingle(ptSize).Value,
                        72, 72));
                }

                CurrentSize = ptSize;
                unsafe
                {
                    var metrics = _face->size->metrics;
                    float ascender = Fixed26Dot6.FromRawValue((int)metrics.ascender).ToSingle();
                    float descender = Fixed26Dot6.FromRawValue((int)metrics.descender).ToSingle();
                    float height = Fixed26Dot6.FromRawValue((int)metrics.height).ToSingle();
                    ScaledMetrics = new FontMetrics(ascender, descender, height);
                }
            }
        }

        public ref GlyphInfo GetGlyphInfo(char c)
        {
            if (!_cacheEntryIndices.TryGetValue(c, out var cachedIndex))
            {
                _cacheEntryIndices[c] = _cachedGlyphs.Count;
                return ref LoadGlyph(c);
            }

            return ref _cachedGlyphs[cachedIndex];
        }

        public float GetKerning(char left, char right)
        {
            unsafe
            {
                long flags = (long)_face->face_flags;
                bool hasKerning = (flags & (1L << 6)) != 0;
                uint idxLeft = FT.FT_Get_Char_Index(_face, left);
                uint idxRight = FT.FT_Get_Char_Index(_face, right);

                FT.CheckResult(FT.FT_Get_Kerning(_face, idxLeft, idxLeft, KerningMode.Default, out var kerning));
                return kerning.X.ToSingle();
            }
        }

        private ref GlyphInfo LoadGlyph(char c)
        {
            ref var glyphInfo = ref _cachedGlyphs.Add();
            unsafe
            {
                var face = _face;
                FT.CheckResult(FT.FT_Load_Char(face, c, LoadFlags.Default));
                var slot = face->glyph;
                var metrics = &face->glyph->metrics;

                glyphInfo.Size = new SizeF((long)metrics->width / 64.0f, (long)metrics->height / 64.0f);
                glyphInfo.Advance = new Vector2(slot->advance.X.ToSingle(), slot->advance.Y.ToSingle());

                FT.CheckResult(FT.FT_Get_Glyph(face->glyph, out var ftGlyph));
                glyphInfo.FTGlyph = ftGlyph;
                return ref glyphInfo;
            }
        }

        public GlyphBitmapInfo Rasterize(ref GlyphInfo glyphInfo, Span<byte> buffer)
        {
            unsafe
            {
                FTVector26Dot6 zero = default;
                var ptr = glyphInfo.FTGlyph;
                FT.CheckResult(FT.FT_Glyph_To_Bitmap(ref ptr, RenderMode.Normal, ref zero, destroy: false));

                var bitmapGlyph = (BitmapGlyph*)ptr;
                var bitmap = &bitmapGlyph->bitmap;
                int size = bitmap->width * bitmap->rows;

                var src = new Span<byte>(bitmap->buffer.ToPointer(), size);
                src.CopyTo(buffer);

                var dimensions = new Primitives.Size((uint)bitmap->width, (uint)bitmap->rows);
                var margin = new Vector2(bitmapGlyph->left, bitmapGlyph->top);
                FT.FT_Done_Glyph(ptr);
                return new GlyphBitmapInfo(dimensions, margin);
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            unsafe
            {
                for (uint i = 0; i < _cachedGlyphs.Count; i++)
                {
                    ref GlyphInfo glyphInfo = ref _cachedGlyphs[i];
                    FT.FT_Done_Glyph(glyphInfo.FTGlyph);
                    glyphInfo.FTGlyph = null;
                }

                _cachedGlyphs.Reset();
                _cacheEntryIndices.Clear();
                FT.FT_Done_Face(_face);
                _face = null;
            }
        }

        ~FontFace()
        {
            Destroy();
        }
    }
}
