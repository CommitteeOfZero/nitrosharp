using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FreeTypeBindings;
using NitroSharp.Graphics;
using Veldrid;

#nullable enable

namespace NitroSharp.Text
{
    internal sealed class GlyphRasterizer : IDisposable
    {
        private readonly bool _enableOutlines;
        private readonly FontContext _metricsContext;
        private readonly FontContext[] _contexts;
        private readonly Channel<FontContext> _freeContexts;
        private readonly Dictionary<FontKey, FontData> _fontDatas;
        private readonly Channel<RasterBatch> _rasterBatches;
        private readonly ConcurrentBag<Exception> _exceptions;
        private volatile int _pendingBatches;

        [StructLayout(LayoutKind.Auto)]
        private readonly struct RasterResult
        {
            public readonly uint GlyphIndex;
            public readonly RasterizedGlyph Glyph;

            public RasterResult(uint glyphIndex, RasterizedGlyph glyph)
                => (GlyphIndex, Glyph) = (glyphIndex, glyph);
        }

        private readonly struct RasterBatch
        {
            public readonly FontKey Font;
            public readonly PtFontSize FontSize;
            public readonly RasterResult[] Results;
            public readonly RasterResult[]? OutlineResults;

            public RasterBatch(
                FontKey font,
                PtFontSize fontSize,
                RasterResult[] results,
                RasterResult[]? outlineResults)
                => (Font, FontSize, Results, OutlineResults)
                    = (font, fontSize, results, outlineResults);
        }

        public GlyphRasterizer(bool enableOutlines)
        {
            _enableOutlines = enableOutlines;
            _metricsContext = new FontContext();
            _contexts = new FontContext[Environment.ProcessorCount];
            _fontDatas = new Dictionary<FontKey, FontData>();
            _exceptions = new ConcurrentBag<Exception>();
            _freeContexts = Channel.CreateUnbounded<FontContext>(
                new UnboundedChannelOptions
                {
                    SingleReader = false,
                    SingleWriter = false
                }
            );
            _rasterBatches = Channel.CreateUnbounded<RasterBatch>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true
                }
            );

            ChannelWriter<FontContext> ctxWriter = _freeContexts.Writer;
            for (int i = 0; i < _contexts.Length; i++)
            {
                var ctx = new FontContext();
                _contexts[i] = ctx;
                ctxWriter.TryWrite(ctx);
            }
        }

        public void AddFonts(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                AddFont(path);
            }
        }

        public FontKey AddFont(string path)
        {
            if (_metricsContext.TryAddFont(path, out FontKey fontKey, out FontFace face))
            {
                foreach (FontContext ctx in _contexts)
                {
                    ctx.TryAddFont(path, out _, out _);
                }
                _fontDatas.Add(fontKey, new FontData(_metricsContext, face));
            }

            return fontKey;
        }

        public FontData GetFontData(FontKey fontKey)
        {
            static FontData notFound(FontKey key)
            {
                throw new ArgumentException($"Font '{key.ToString()}' has not been loaded.");
            }

            return _fontDatas.TryGetValue(fontKey, out FontData? data)
                ? data : notFound(fontKey);
        }

        public void RequestGlyphs(
            FontKey font,
            PtFontSize fontSize,
            ReadOnlySpan<PositionedGlyph> glyphs,
            TextureCache textureCache)
        {
            var newGlyphIndices = new List<uint>();
            FontData fontData = GetFontData(font);
            var invalidHandle = TextureCacheHandle.Invalid;
            foreach (PositionedGlyph glyph in glyphs)
            {
                var key = new GlyphCacheKey(glyph.Index, fontSize);
                if (fontData.TryGetCachedGlyph(key, out GlyphCacheEntry cacheEntry))
                {
                    if (!cacheEntry.IsRegular) { continue; }
                    if (textureCache.RequestEntry(cacheEntry.TextureCacheHandle))
                    {
                        if (!_enableOutlines ||
                            textureCache.RequestEntry(cacheEntry.OutlineTextureCacheHandle))
                        {
                            continue;
                        }
                    }
                }
                fontData.UpsertCachedGlyph(key, GlyphCacheEntry.Pending());
                newGlyphIndices.Add(glyph.Index);
            }

            Interlocked.Increment(ref _pendingBatches);
            Task.Run(() => RasterizeBatch(font, fontSize, newGlyphIndices))
                .ContinueWith(t => _exceptions.Add(t.Exception!),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        public ValueTask ResolveGlyphs(TextureCache textureCache)
        {
            return _pendingBatches == 0
                ? default
                : new ValueTask(DoResolveGlyphs(textureCache));
        }

        private async Task DoResolveGlyphs(TextureCache textureCache)
        {
            if (!_exceptions.IsEmpty)
            {
                Exception[] exceptions = _exceptions.ToArray();
                throw exceptions.Length == 1
                    ? exceptions[0]
                    : new AggregateException(exceptions);
            }

            ChannelReader<RasterBatch> batches = _rasterBatches.Reader;
            while (_pendingBatches > 0)
            {
                RasterBatch batch = await batches.ReadAsync();
                Interlocked.Decrement(ref _pendingBatches);
                uploadBatch(batch);
            }

            void uploadBatch(RasterBatch batch)
            {
                FontData fontData = GetFontData(batch.Font);
                RasterResult[] results = batch.Results;
                for (uint i = 0; i < results.Length; i++)
                {
                    ref readonly RasterResult rasterRes = ref results[i];
                    ref readonly RasterizedGlyph glyph = ref rasterRes.Glyph;
                    var key = new GlyphCacheKey(rasterRes.GlyphIndex, batch.FontSize);
                    if (glyph.Width == 0 || glyph.Height == 0)
                    {
                        fontData.UpsertCachedGlyph(key, GlyphCacheEntry.Blank());
                        continue;
                    }
                    var size = new Primitives.Size(glyph.Width, glyph.Height);
                    var handle = TextureCacheHandle.Invalid;
                    textureCache.Update<byte>(ref handle, PixelFormat.R8_UNorm, size, glyph.Bytes);

                    var outlineHandle = TextureCacheHandle.Invalid;
                    if (batch.OutlineResults != null)
                    {
                        rasterRes = ref batch.OutlineResults[i];
                        ref readonly RasterizedGlyph outline = ref rasterRes.Glyph;
                        Debug.Assert(outline.Width != 0 && outline.Height != 0);
                        size = new Primitives.Size(outline.Width, outline.Height);
                        float horOffset = outline.Left;
                        float verOffset = -(outline.Height - glyph.Height) - outline.Bottom;
                        outlineHandle = TextureCacheHandle.Invalid;
                        textureCache.Update<byte>(
                            ref outlineHandle,
                            PixelFormat.R8_G8_B8_A8_UNorm,
                            size,
                            outline.Bytes,
                            userData: new Vector3(new Vector2(horOffset, verOffset), 0)
                        );
                    }

                    fontData.UpsertCachedGlyph(key, GlyphCacheEntry.Regular(handle, outlineHandle));
                }
            }
        }

        unsafe struct RgbaPixel
        {
            public fixed byte Channels[4];
        }

        private async Task RasterizeBatch(FontKey font, PtFontSize fontSize, List<uint> indices)
        {
            async Task<RasterResult> rasterizeGlyph(
                FontKey font,
                PtFontSize fontSize,
                uint index)
            {
                FontContext ctx = await _freeContexts.Reader.ReadAsync();
                FontFace face = ctx.GetFontFace(font)!;
                RasterizedGlyph g = ctx.RasterizeGlyph(face, fontSize, index);
                _freeContexts.Writer.TryWrite(ctx);
                return new RasterResult(index, g);
            }

            async Task<RasterResult> produceOutline(
                FontKey font,
                PtFontSize fontSize,
                uint glyphIndex)
            {
                NativeBitmapGlyph[] strokes = await Task.WhenAll(new[]
                {
                    Task.Run(() => stroke(font, fontSize, glyphIndex, radius: 1)),
                    Task.Run(() => stroke(font, fontSize, glyphIndex, radius: 2)),
                    Task.Run(() => stroke(font, fontSize, glyphIndex, radius: 3)),
                    Task.Run(() => stroke(font, fontSize, glyphIndex, radius: 4))
                });

                RasterizedGlyph outline = mergeStrokes(strokes);
                foreach (NativeBitmapGlyph g in strokes)
                {
                    g.Dispose();
                }
                return new RasterResult(glyphIndex, outline);
            }

            static unsafe RasterizedGlyph mergeStrokes(NativeBitmapGlyph[] strokes)
            {
                NativeBitmapGlyph largest = strokes[^1];
                var buffer = new byte[largest.Width * largest.Height * 4];
                Span<RgbaPixel> pixels = MemoryMarshal.Cast<byte, RgbaPixel>(buffer);
                for (int i = 0; i < 4; i++)
                {
                    NativeBitmapGlyph stroke = strokes[i];
                    ReadOnlySpan<byte> bytes = stroke.Bytes;
                    uint dstStartX = (uint)(-largest.Left + stroke.Left);
                    uint dstStartY = (uint)(largest.Height - stroke.Height - stroke.Bottom + largest.Bottom);
                    for (uint srcY = 0; srcY < stroke.Height; srcY++)
                    {
                        for (uint srcX = 0; srcX < stroke.Width; srcX++)
                        {
                            int idx = (int)(largest.Width * (dstStartY + srcY) + dstStartX + srcX);
                            pixels[idx].Channels[i] = bytes[(int)(srcY * stroke.Width + srcX)];
                        }
                    }
                }
                return new RasterizedGlyph(
                    buffer,
                    largest.ActualTop,
                    largest.Left,
                    largest.Width,
                    largest.Height,
                    largest.Height,
                    largest.Bottom
                );
            }

            async Task<NativeBitmapGlyph> stroke(
                FontKey font,
                PtFontSize fontSize,
                uint index,
                uint radius)
            {
                FontContext ctx = await _freeContexts.Reader.ReadAsync();
                FontFace face = ctx.GetFontFace(font)!;
                NativeBitmapGlyph g = ctx.StrokeGlyph(face, fontSize, index, radius);
                _freeContexts.Writer.TryWrite(ctx);
                return g;
            }

            var tasks = new Task<RasterResult>[indices.Count];
            int i = 0;
            foreach (uint index in indices)
            {
                tasks[i++] = Task.Run(() => rasterizeGlyph(font, fontSize, index));
            }

            Task<RasterResult>[]? outlineTasks = null;
            if (_enableOutlines)
            {
                outlineTasks = new Task<RasterResult>[indices.Count];
                i = 0;
                foreach (uint index in indices)
                {
                    outlineTasks[i++] = Task.Run(() => produceOutline(font, fontSize, index));
                }
            }

            RasterResult[] results = await Task.WhenAll(tasks);
            RasterResult[]? outlineResults = null;
            if (_enableOutlines)
            {
                Debug.Assert(outlineTasks != null);
                outlineResults = await Task.WhenAll(outlineTasks);
            }

            _rasterBatches.Writer.TryWrite(
                new RasterBatch(font, fontSize, results, outlineResults)
            );
        }

        public void Dispose()
        {
            _fontDatas.Clear();
            _metricsContext.Dispose();
            foreach (FontContext ctx in _contexts)
            {
                ctx.Dispose();
            }
        }
    }

    internal readonly struct GlyphCacheKey : IEquatable<GlyphCacheKey>
    {
        public readonly uint Index;
        public readonly PtFontSize FontSize;

        public GlyphCacheKey(uint index, PtFontSize fontSize)
            => (Index, FontSize) = (index, fontSize);

        public bool Equals(GlyphCacheKey other)
            => Index == other.Index && FontSize.Equals(other.FontSize);

        public override int GetHashCode()
            => HashCode.Combine(Index, FontSize);
    }

    internal enum GlyphCacheEntryKind
    {
        Regular,
        Pending,
        Blank
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct GlyphCacheEntry
    {
        private readonly TextureCacheHandle _handle;
        private readonly TextureCacheHandle _outlineHandle;
        public readonly GlyphCacheEntryKind Kind;

        public GlyphCacheEntry(
            GlyphCacheEntryKind kind,
            TextureCacheHandle textureCacheHandle,
            TextureCacheHandle outlineTextureCacheHandle)
        {
            Kind = kind;
            _handle = textureCacheHandle;
            _outlineHandle = outlineTextureCacheHandle;
        }

        public bool IsPending => Kind == GlyphCacheEntryKind.Pending;
        public bool IsRegular => Kind == GlyphCacheEntryKind.Regular;

        public static GlyphCacheEntry Regular(
            TextureCacheHandle textureCacheHandle,
            TextureCacheHandle outlineTextureCacheHandle)
        {
            return new GlyphCacheEntry(
               GlyphCacheEntryKind.Regular,
               textureCacheHandle,
               outlineTextureCacheHandle
           );
        }

        public static GlyphCacheEntry Pending() =>
            new GlyphCacheEntry(
                GlyphCacheEntryKind.Pending,
                TextureCacheHandle.Invalid,
                TextureCacheHandle.Invalid
            );

        public static GlyphCacheEntry Blank() =>
            new GlyphCacheEntry(
                GlyphCacheEntryKind.Blank,
                TextureCacheHandle.Invalid,
                TextureCacheHandle.Invalid
            );

        public TextureCacheHandle TextureCacheHandle
        {
            get
            {
                Debug.Assert(IsRegular);
                return _handle;
            }
        }

        public TextureCacheHandle OutlineTextureCacheHandle
        {
            get
            {
                Debug.Assert(IsRegular);
                return _outlineHandle;
            }
        }
    }

    internal sealed class FontData
    {
        private readonly FontContext _fontContext;
        private readonly FontFace _fontFace;
        private readonly Dictionary<char, uint> _glyphIndexCache;
        private readonly Dictionary<(uint index, PtFontSize fontSize), GlyphDimensions> _dimensionsCache;
        private readonly Dictionary<GlyphCacheKey, GlyphCacheEntry> _glyphCache;

        public FontData(FontContext fontContext, FontFace fontFace)
        {
            _fontContext = fontContext;
            _fontFace = fontFace;
            _glyphIndexCache = new Dictionary<char, uint>();
            _dimensionsCache = new Dictionary<(uint, PtFontSize), GlyphDimensions>();
            _glyphCache = new Dictionary<GlyphCacheKey, GlyphCacheEntry>();
        }

        public void UpsertCachedGlyph(GlyphCacheKey key, in GlyphCacheEntry entry)
            => _glyphCache[key] = entry;

        public bool TryGetCachedGlyph(GlyphCacheKey cacheKey, out GlyphCacheEntry cacheEntry)
            => _glyphCache.TryGetValue(cacheKey, out cacheEntry);

        public VerticalMetrics GetVerticalMetrics(PtFontSize fontSize)
            => _fontContext.GetFontMetrics(_fontFace, fontSize);

        public uint GetGlyphIndex(char ch)
        {
            if (!_glyphIndexCache.TryGetValue(ch, out uint index))
            {
                index = _fontContext.GetGlyphIndex(_fontFace, ch);
                _glyphIndexCache.Add(ch, index);
            }
            return index;
        }

        public GlyphDimensions GetGlyphDimensions(uint index, PtFontSize fontSize)
        {
            if (!_dimensionsCache.TryGetValue((index, fontSize), out GlyphDimensions dim))
            {
                dim = _fontContext.GetGlyphDimensions(_fontFace, fontSize, index);
                _dimensionsCache.Add((index, fontSize), dim);
            }
            return dim;
        }
    }

    internal readonly struct RasterizedGlyph
    {
        public readonly byte[] Bytes;
        public readonly int Top;
        public readonly int Left;
        public readonly uint Width;
        public readonly uint Height;
        public readonly uint MinHeight;
        public readonly int Bottom;

        public RasterizedGlyph(
            byte[] bytes,
            int top,
            int left,
            uint width,
            uint height,
            uint minHeight = 0,
            int bottom = 0)
        {
            Bytes = bytes;
            Top = top;
            Left = left;
            Width = width;
            Height = height;
            MinHeight = minHeight;
            Bottom = bottom;
        }
    }

    internal readonly unsafe struct NativeBitmapGlyph : IDisposable
    {
        private readonly BitmapGlyph* _ftGlyph;
        public readonly int Top;
        public readonly int Left;
        public readonly uint Width;
        public readonly uint Height;

        public NativeBitmapGlyph(BitmapGlyph* ftGlyph, int bottom, int actualTop)
        {
            _ftGlyph = ftGlyph;
            Bottom = bottom;
            Top = _ftGlyph->top;
            Left = _ftGlyph->left;
            Width = (uint)_ftGlyph->bitmap.width;
            Height = (uint)_ftGlyph->bitmap.rows;
            ActualTop = actualTop;
        }

        public ReadOnlySpan<byte> Bytes =>
            new ReadOnlySpan<byte>(
                _ftGlyph->bitmap.buffer.ToPointer(),
                (int)(Width * Height)
            );

        public readonly int ActualTop;
        public readonly int Bottom;

        public readonly void Dispose()
        {
            var ptr = (Glyph*)_ftGlyph;
            FT.FT_Done_Glyph(ptr);
        }
    }

    internal sealed unsafe class FontFace : IDisposable
    {
        private Face* _face;

        public FontFace(Face* face)
        {
            _face = face;
            FontFamily = Marshal.PtrToStringAnsi(face->family_name)!;
            var style = (StyleFlags)face->style_flags;
            if (style == 0)
            {
                Style = FontStyle.Regular;
            }
            if ((style & StyleFlags.Italic) == StyleFlags.Italic)
            {
                Style |= FontStyle.Italic;
            }
            if ((style & StyleFlags.Bold) == StyleFlags.Bold)
            {
                Style |= FontStyle.Bold;
            }
        }

        public Face* FTFace => _face;
        public string FontFamily { get; }
        public FontStyle Style { get; }

        public void Dispose()
        {
            FT.FT_Done_Face(_face);
            _face = null;
        }
    }

    internal sealed unsafe class FontContext : IDisposable
    {
        private IntPtr _freetypeLib;
        private IntPtr _stroker;
        private PtFontSize _lastSize;
        private readonly Dictionary<FontKey, FontFace> _faces;

        public FontContext()
        {
            _faces = new Dictionary<FontKey, FontFace>();
            FT.FT_Init_FreeType(out _freetypeLib);
            FT.FT_Stroker_New(_freetypeLib, out _stroker);
        }

        public bool TryAddFont(string path, out FontKey fontKey, out FontFace fontFace)
        {
            FT.CheckResult(FT.FT_New_Face(_freetypeLib, path, 0, out Face* ftFace));
            fontFace = new FontFace(ftFace);
            fontKey = new FontKey(fontFace.FontFamily, fontFace.Style);
            if (!_faces.TryAdd(fontKey, fontFace))
            {
                fontFace.Dispose();
                fontFace = _faces[fontKey];
                return false;
            }
            return true;
        }

        public FontFace? GetFontFace(FontKey fontKey)
            => _faces.TryGetValue(fontKey, out FontFace? face) ? face : null;

        public uint GetGlyphIndex(FontFace fontFace, char c)
            => FT.FT_Get_Char_Index(fontFace.FTFace, c);

        public VerticalMetrics GetFontMetrics(FontFace font, PtFontSize fontSize)
        {
            Face* ftFace = font.FTFace;
            SetSize(ftFace, fontSize);
            SizeMetrics metrics = ftFace->size->metrics;
            float ascender = Fixed26Dot6.FromRawValue((int)metrics.ascender).ToSingle();
            float descender = Fixed26Dot6.FromRawValue((int)metrics.descender).ToSingle();
            float height = Fixed26Dot6.FromRawValue((int)metrics.height).ToSingle();
            return new VerticalMetrics(ascender, descender, height);
        }

        public uint GetGlyphIndex(FontFace fontFace, PtFontSize fontSize, char ch)
        {
            Face* ftFace = fontFace.FTFace;
            SetSize(fontFace.FTFace, fontSize);
            return FT.FT_Get_Char_Index(ftFace, ch);
        }

        public GlyphDimensions GetGlyphDimensions(FontFace fontFace, PtFontSize fontSize, uint index)
        {
            Face* ftFace = fontFace.FTFace;
            SetSize(fontFace.FTFace, fontSize);
            FT.CheckResult(FT.FT_Load_Glyph(ftFace, index, LoadFlags.Default));
            GlyphSlot* glyph = ftFace->glyph;
            return new GlyphDimensions(
                glyph->bitmap_top,
                glyph->bitmap_left,
                (uint)glyph->bitmap.width,
                (uint)glyph->bitmap.rows,
                glyph->advance.X.ToSingle()
            );
        }

        public RasterizedGlyph RasterizeGlyph(FontFace fontFace, PtFontSize fontSize, uint index)
        {
            Face* ftFace = fontFace.FTFace;
            SetSize(fontFace.FTFace, fontSize);
            FT.CheckResult(FT.FT_Load_Glyph(ftFace, index, LoadFlags.Default));
            GlyphSlot* glyph = ftFace->glyph;
            (int bitmapLeft, int bitmapTop) = (glyph->bitmap_left, glyph->bitmap_top);
            (int width, int height) = (glyph->bitmap.width, glyph->bitmap.rows);

            ref Outline outline = ref glyph->outline;
            FT.FT_Outline_Translate(
                ref outline,
                xOffset: (IntPtr)(-bitmapLeft * 64),
                yOffset: (IntPtr)((height - bitmapTop) * 64)
            );

            int bufferSize = width * height;
            var buffer = bufferSize > 0 ? new byte[width * height] : Array.Empty<byte>();
            if (bufferSize > 0)
            {
                fixed (byte* ptr = &buffer[0])
                {
                    var bmp = new Bitmap
                    {
                        buffer = new IntPtr(ptr),
                        width = width,
                        pitch = width,
                        rows = height,
                        pixel_mode = PixelMode.Gray,
                        num_grays = 256
                    };

                    FT.CheckResult(
                        FT.FT_Outline_Get_Bitmap(_freetypeLib, ref outline, ref bmp)
                    );
                }
            }

            return new RasterizedGlyph(buffer, bitmapTop, bitmapLeft, (uint)width, (uint)height);
        }

        public NativeBitmapGlyph StrokeGlyph(FontFace fontFace, PtFontSize fontSize, uint index, uint radius)
        {
            IntPtr stroker = _stroker;
            Face* ftFace = fontFace.FTFace;
            SetSize(fontFace.FTFace, fontSize);
            FT.CheckResult(FT.FT_Load_Glyph(ftFace, index, LoadFlags.Default));

            GlyphSlot* slot = ftFace->glyph;
            ref Outline outline = ref slot->outline;
            FT.FT_Outline_Translate(
                ref outline,
                xOffset: (IntPtr)(-(slot->bitmap_left) * 64),
                yOffset: (IntPtr)((slot->bitmap.rows - slot->bitmap_top) * 64)
            );

            FT.CheckResult(FT.FT_Get_Glyph(ftFace->glyph, out Glyph* glyph));
            FT.FT_Stroker_Set(
                stroker,
                radius: 64 * (int)radius,
                StrokerLineCap.Round,
                StrokerLineJoin.Round,
                miter_limit: IntPtr.Zero
            );
            FT.CheckResult(FT.FT_Glyph_Stroke(ref glyph, stroker, destroy: true));
            FTVector26Dot6 origin = default;
            FT.CheckResult(
                FT.FT_Glyph_To_Bitmap(ref glyph, RenderMode.Normal, ref origin, destroy: true)
            );

            FT.FT_Glyph_Get_CBox(glyph, GlyphBBoxMode.Pixels, out BBox cbox);


            return new NativeBitmapGlyph((BitmapGlyph*)glyph, cbox.Bottom, cbox.Top);
        }

        private void SetSize(Face* ftFace, PtFontSize size)
        {
            if (!size.Equals(_lastSize))
            {
                FT.CheckResult(FT.FT_Set_Char_Size(
                    ftFace,
                    char_width: (IntPtr)0,
                    char_height: (IntPtr)size.Value.Value,
                    72, 72
                ));
                _lastSize = size;
            }
        }

        public void Dispose()
        {
            foreach (FontFace face in _faces.Values)
            {
                face.Dispose();
            }

            _faces.Clear();
            FT.FT_Stroker_Done(_stroker);
            _stroker = IntPtr.Zero;
            FT.FT_Done_FreeType(_freetypeLib);
            _freetypeLib = IntPtr.Zero;
        }
    }
}
