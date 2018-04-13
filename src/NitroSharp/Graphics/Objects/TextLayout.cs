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

        private CommandList _cl;
        private Texture _layoutStaging;
        private Texture _layoutTexture;
        private TextureView _textureView;
        private NativeMemory _nativeBuffer;

        private readonly HashSet<uint> _glyphsToUpdate;
        private readonly bool _hidden;

        public TextLayout(TextRun[] text, uint textLength, FontFamily fontFamily, in Size maxBounds, bool hidden = true)
        {
            _bounds = maxBounds;
            _fontFamily = fontFamily;

            _builder = new LayoutBuilder(fontFamily, textLength, maxBounds);
            _builder.Append(text);
            Priority = int.MaxValue;

            _glyphsToUpdate = new HashSet<uint>();
            _hidden = hidden;
        }

        public uint GlyphCount => _builder.Glyphs.Count;

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
            var texturePool = renderContext.TexturePool;
            _layoutStaging = texturePool.RentStaging(_bounds, clearMemory: true);
            _layoutTexture = texturePool.RentSampled(_bounds);
            _textureView = renderContext.Factory.CreateTextureView(_layoutTexture);
            _nativeBuffer = NativeMemory.Allocate(128 * 128);

            _cl = renderContext.Factory.CreateCommandList();
            _cl.Begin();
            _cl.CopyTexture(_layoutStaging, _layoutTexture);
            _cl.End();
            renderContext.Device.SubmitCommands(_cl);

            if (!_hidden)
            {
                Redraw(renderContext);
            }
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
            ref var glyph = ref _builder.Glyphs[index];
            if (glyph.Color.A == 0)
            {
                return;
            }

            var font = _fontFamily.GetFace(glyph.FontStyle);
            ref var glyphInfo = ref font.GetGlyphInfo(glyph.Char);

            var bitmapInfo = font.Rasterize(ref glyphInfo, srcBuffer);
            var dimensions = bitmapInfo.Dimensions;
            if (dimensions.Width == 0)
            {
                return;
            }

            var margin = bitmapInfo.Margin;
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

        public override void Render(RenderContext renderContext)
        {
            if (_glyphsToUpdate.Count > 0)
            {
                Redraw(renderContext, _glyphsToUpdate);
                _glyphsToUpdate.Clear();
            }

            var rect = new RectangleF(0, 0, _bounds.Width, _bounds.Height);
            renderContext.Canvas.DrawImage(_textureView, rect, rect, RgbaFloat.White);
        }

        public override void Destroy(RenderContext renderContext)
        {
            _cl.Dispose();
            _nativeBuffer.Dispose();

            _textureView.Dispose();
            renderContext.TexturePool.Return(_layoutTexture);
            renderContext.TexturePool.Return(_layoutStaging);
        }
    }
}
