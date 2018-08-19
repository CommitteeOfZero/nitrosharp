using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Objects
{
    internal class TextLayout : Visual
    {
        private readonly LayoutBuilder _builder;
        private readonly Size _bounds;
        private readonly FontFamily _fontFamily;

        private GraphicsDevice _gd;
        private CommandList _cl;
        private Texture _layoutStaging;
        private Texture _layoutTexture;
        private TextureView _textureView;

        private NativeMemory _nativeBuffer;
        private readonly HashSet<uint> _glyphsToUpdate;

        public TextLayout(uint initialGlyphCapacity, FontFamily fontFamily, in Size maxBounds)
        {
            _bounds = maxBounds;
            _fontFamily = fontFamily;

            _builder = new LayoutBuilder(fontFamily, initialGlyphCapacity, maxBounds);
            _glyphsToUpdate = new HashSet<uint>();
            Priority = 100000;
        }

        public uint GlyphCount => _builder.Glyphs.Count;

        public void Append(TextRun textRun, bool display = false)
        {
            uint start = GlyphCount;
            _builder.Append(textRun);
            if (display)
            {
                for (uint i = start; i < start + textRun.Text.Length; i++)
                {
                    _glyphsToUpdate.Add(i);
                }
            }
        }

        public void StartNewLine()
        {
            _builder.StartNewLine();
        }

        public void Clear()
        {
            _builder.Clear();
            _glyphsToUpdate.Clear();
            if (_gd != null)
            {
                ClearTextures();
            }
        }

        private void ClearTextures()
        {
            _gd.InitStagingTexture(_layoutStaging);
            using (var cl = _gd.ResourceFactory.CreateCommandList())
            {
                cl.Name = "ClearTextures";
                cl.Begin();
                cl.CopyTexture(_layoutStaging, _layoutTexture);
                cl.End();
                _gd.SubmitCommands(cl);
            }
        }

        public ref LayoutGlyph MutateGlyph(uint index)
        {
            if (index >= _builder.Glyphs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _glyphsToUpdate.Add(index);
            return ref _builder.Glyphs[index];
        }

        public Span<LayoutGlyph> MutateSpan(uint start, uint length)
        {
            if ((start + length) > _builder.Glyphs.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            var span = _builder.Glyphs.AsSpan().Slice((int)start, (int)length);
            for (uint i = start; i < start + length; i++)
            {
                _glyphsToUpdate.Add(i);
            }

            return span;
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            _gd = renderContext.Device;
            _cl = renderContext.ResourceFactory.CreateCommandList();

            var texturePool = renderContext.TexturePool;
            _layoutStaging = texturePool.RentStaging(_bounds);
            _layoutTexture = texturePool.RentSampled(_bounds);
            _textureView = renderContext.ResourceFactory.CreateTextureView(_layoutTexture);
            _nativeBuffer = NativeMemory.Allocate(128 * 128);

            ClearTextures();
        }

        public override void Render(RenderContext renderContext)
        {
            if (_glyphsToUpdate.Count > 0)
            {
                Redraw(renderContext, _glyphsToUpdate);
                _glyphsToUpdate.Clear();
            }

            var rect = new RectangleF(0, 0, _bounds.Width, _bounds.Height);
            renderContext.QuadBatcher.DrawImage(_textureView, rect, rect, ref _color, 0);
        }

        private void Redraw(RenderContext renderContext, ISet<uint> glyphIndices = null)
        {
            var device = renderContext.Device;
            var buffer = _nativeBuffer.AsSpan<byte>();
            var map = device.Map<RgbaByte>(_layoutStaging, MapMode.Write);
            _cl.Begin();
            if (glyphIndices != null)
            {
                foreach (uint index in glyphIndices)
                {
                    DrawGlyph(_cl, index, buffer, map);
                }
            }
            else
            {
                for (uint i = 0; i < _builder.Glyphs.Count; i++)
                {
                    DrawGlyph(_cl, i, buffer, map);
                }
            }
            _cl.End();
            device.Unmap(_layoutStaging);
            device.SubmitCommands(_cl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGlyph(
            CommandList commandList, uint index,
            Span<byte> srcBuffer, MappedResourceView<RgbaByte> dstBuffer)
        {
            ref LayoutGlyph glyph = ref _builder.Glyphs[index];
            if (glyph.Color.A == 0)
            {
                return;
            }

            FontFace font = _fontFamily.GetFace(glyph.FontStyle);
            ref GlyphInfo glyphInfo = ref font.GetGlyphInfo(glyph.Char);

            GlyphBitmapInfo bitmapInfo = font.Rasterize(ref glyphInfo, srcBuffer);
            Size dimensions = bitmapInfo.Dimensions;
            if (dimensions.Width == 0)
            {
                return;
            }

            Vector2 margin = bitmapInfo.Margin;
            // TODO: remove the hardcoded top margin.
            var pos = new Vector2(glyph.Position.X + margin.X, 28 + glyph.Position.Y - margin.Y);
            var color = glyph.Color;
            for (uint y = 0; y < dimensions.Height; y++)
            {
                for (uint x = 0; x < dimensions.Width; x++)
                {
                    int srcIndex = (int)(y * dimensions.Width + x);
                    if (srcBuffer[srcIndex] != 0x00 && color.A != 0)
                    {
                        var rgbaByte = new RgbaByte(
                            (byte)(255 * color.R),
                            (byte)(255 * color.G),
                            (byte)(255 * color.B),
                            (byte)(srcBuffer[srcIndex] * color.A));

                        dstBuffer[(uint)pos.X + x, (uint)pos.Y + y] = rgbaByte;
                    }
                }
            }

            commandList.CopyTexture(
                _layoutStaging, (uint)pos.X, (uint)pos.Y, 0, 0, 0, _layoutTexture,
                (uint)pos.X, (uint)pos.Y, 0, 0, 0, dimensions.Width, dimensions.Height, 1, 1);
        }

        public override void DestroyDeviceObjects(RenderContext renderContext)
        {
            _cl.Dispose();
            _nativeBuffer.Dispose();
            _textureView.Dispose();

            renderContext.TexturePool.Return(_layoutTexture);
            renderContext.TexturePool.Return(_layoutStaging);

            for (uint i = 0; i < GlyphCount; i++)
            {
                _glyphsToUpdate.Add(i);
            }
        }
    }
}
