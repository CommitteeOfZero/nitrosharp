using System;
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
        private BindableTexture _layoutTexture;
        private NativeMemory _nativeBuffer;

        public TextLayout(TextRun[] text, uint textLength, FontFamily fontFamily, in Size maxBounds)
        {
            _bounds = maxBounds;
            _fontFamily = fontFamily;

            _builder = new LayoutBuilder(fontFamily, textLength, maxBounds);
            _builder.Append(text);
            Priority = int.MaxValue;
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            var device = renderContext.Device;
            var factory = renderContext.Factory;
            _cl = factory.CreateCommandList();

            _layoutStaging = factory.CreateTexture(
                TextureDescription.Texture2D(
                    _bounds.Width, _bounds.Height, 1, 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            var data = device.Map<RgbaByte>(_layoutStaging, MapMode.Write);
            unsafe
            {
                var res = data.MappedResource;
                Unsafe.InitBlock(res.Data.ToPointer(), 0x00, res.SizeInBytes);
            }
            device.Unmap(_layoutStaging);

            _layoutTexture = new BindableTexture(factory,
                factory.CreateTexture(TextureDescription.Texture2D(
                    _bounds.Width, _bounds.Height, 1, 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled)));

            _nativeBuffer = NativeMemory.Allocate(1000 * 1000);
            Update(renderContext, _builder.Glyphs.AsReadonlySpan());
        }

        private void Update(RenderContext renderContext, ReadOnlySpan<LayoutGlyph> glyphs)
        {
            var device = renderContext.Device;

            var buffer = _nativeBuffer.AsSpan<byte>();
            var data = device.Map<RgbaByte>(_layoutStaging, MapMode.Write);
            for (uint i = 0; i < _builder.Glyphs.Count; i++)
            {
                ref var glyph = ref _builder.Glyphs[i];
                var font = _fontFamily.GetFace(glyph.FontStyle);
                ref var glyphInfo = ref font.GetGlyphInfo(glyph.Char);
                
                var bitmapInfo = font.Rasterize(ref glyphInfo, buffer, RgbaByte.White);
                var dimensions = bitmapInfo.Dimensions;
                if (dimensions.Width == 0)
                {
                    continue;
                }

                var margin = bitmapInfo.Margin;

                var pos = new Vector2(glyph.Position.X + margin.X, 28 + glyph.Position.Y - margin.Y);
                var color = glyph.Color ?? RgbaFloat.White;
                for (uint y = 0; y < dimensions.Height; y++)
                {
                    for (uint x = 0; x < dimensions.Width; x++)
                    {
                        ref var dstPixel = ref data[(uint)(pos.X + x), (uint)(pos.Y + y)];
                        if (dstPixel.Equals(default))
                        {
                            int srcIndex = (int)(y * dimensions.Width + x);
                            ref var srcValue = ref buffer[srcIndex];
                            var rgbaByte = new RgbaByte((byte)(255 * color.R), (byte)(255 * color.G), (byte)(255 * color.B), srcValue);
                            data[(uint)pos.X + x, (uint)pos.Y + y] = rgbaByte;
                        }
                    }
                }
            }
            device.Unmap(_layoutStaging);

            _cl.Begin();
            _cl.CopyTexture(_layoutStaging, _layoutTexture);
            _cl.End();
            device.SubmitCommands(_cl);
        }

        public override void Render(RenderContext renderContext)
        {
            renderContext.Canvas.DrawImage(_layoutTexture.GetTextureView(), 0, 0, RgbaFloat.White);
        }

        public override void Destroy(RenderContext renderContext)
        {
            _layoutStaging.Dispose();
            _layoutTexture.Dispose();
            _nativeBuffer.Dispose();
        }
    }
}
