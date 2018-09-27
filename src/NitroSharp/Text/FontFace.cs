using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeBindings;
using NitroSharp.Primitives;

namespace NitroSharp.Text
{
    internal sealed class FontFace : IDisposable
    {
        private unsafe Face* _face;
        private readonly Dictionary<char, GlyphInfo> _glyphCache;

        public unsafe FontFace(Face* face)
        {
            _face = face;
            _glyphCache = new Dictionary<char, GlyphInfo>(512);

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

                    CurrentSize = ptSize;
                    SizeMetrics metrics = _face->size->metrics;
                    float ascender = Fixed26Dot6.FromRawValue((int)metrics.ascender).ToSingle();
                    float descender = Fixed26Dot6.FromRawValue((int)metrics.descender).ToSingle();
                    float height = Fixed26Dot6.FromRawValue((int)metrics.height).ToSingle();
                    ScaledMetrics = new FontMetrics(ascender, descender, height);
                }
            }
        }

        public void GetGlyphInfo(char c, out GlyphInfo glyphInfo)
        {
            if (!_glyphCache.TryGetValue(c, out glyphInfo))
            {
                LoadGlyph(c, out glyphInfo);
                _glyphCache[c] = glyphInfo;
            }
        }

        private void LoadGlyph(char c, out GlyphInfo glyphInfo)
        {
            unsafe
            {
                Face* face = _face;
                FT.CheckResult(FT.FT_Load_Char(face, c, LoadFlags.Default));
                GlyphSlot* slot = face->glyph;
                GlyphMetrics* metrics = &face->glyph->metrics;

                glyphInfo.Size = new SizeF((long)metrics->width / 64.0f, (long)metrics->height / 64.0f);
                glyphInfo.Advance = new Vector2(slot->advance.X.ToSingle(), slot->advance.Y.ToSingle());

                FT.CheckResult(FT.FT_Get_Glyph(face->glyph, out var ftGlyph));
                glyphInfo.FTGlyph = ftGlyph;
            }
        }

        public GlyphBitmapInfo RasterizeGlyph(ref GlyphInfo glyphInfo, Span<byte> buffer)
        {
            unsafe
            {
                FTVector26Dot6 zero = default;
                Glyph* ptr = glyphInfo.FTGlyph;
                FT.CheckResult(FT.FT_Glyph_To_Bitmap(ref ptr, RenderMode.Normal, ref zero, destroy: false));

                var bitmapGlyph = (BitmapGlyph*)ptr;
                Bitmap* bitmap = &bitmapGlyph->bitmap;
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
                foreach (GlyphInfo gi in _glyphCache.Values)
                {
                    FT.FT_Done_Glyph(gi.FTGlyph);
                }

                _glyphCache.Clear();
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
