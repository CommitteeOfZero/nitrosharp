using System;
using System.Numerics;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Renderers
{
    internal sealed class TextRenderer : IDisposable
    {
        private readonly World _world;
        private readonly RenderContext _renderContext;

        private const ushort ArraySize = World.InitialTextLayoutCount;

        private Texture[] _layoutTextures = new Texture[ArraySize];
        private Texture[] _stagingTextures = new Texture[ArraySize];
        private TextureView[] _textureViews = new TextureView[ArraySize];

        private NativeMemory _nativeBuffer = NativeMemory.Allocate(256 * 256 * 4);

        public TextRenderer(World world, RenderContext renderContext)
        {
            _world = world;
            _renderContext = renderContext;
            world.TextInstanceAdded += OnTextInstanceAdded;
            world.TextInstanceRemoved += OnTextInstanceRemoved;
        }

        private void OnTextInstanceAdded(Entity entity)
        {
            int requiredCapacity = entity.Index + 1;
            ArrayUtil.EnsureCapacity(ref _stagingTextures, requiredCapacity);
            ArrayUtil.EnsureCapacity(ref _layoutTextures, requiredCapacity);
            ArrayUtil.EnsureCapacity(ref _textureViews, requiredCapacity);

            TextLayout layout = _world.TextInstances.Layouts.GetValue(entity);
            RgbaTexturePool texturePool = _renderContext.TexturePool;
            _stagingTextures[entity.Index] = texturePool.RentStaging(layout.MaxBounds);
            Texture sampled = texturePool.RentSampled(layout.MaxBounds);
            _layoutTextures[entity.Index] = sampled;
            _textureViews[entity.Index] = _renderContext.ResourceFactory.CreateTextureView(sampled);
        }

        private void OnTextInstanceRemoved(Entity entity)
        {
            RgbaTexturePool texturePool = _renderContext.TexturePool;
            ref Texture staging = ref _stagingTextures[entity.Index];
            ref Texture sampled = ref _layoutTextures[entity.Index];
            texturePool.Return(staging);
            texturePool.Return(sampled);
            staging = null;
            sampled = null;

            ref TextureView view = ref _textureViews[entity.Index];
            _renderContext.Device.DisposeWhenIdle(view);
            view = null;
        }

        public void RenderTextLayouts(TextInstances textInstances)
        {
            TransformProcessor.ProcessTransforms(_world, textInstances);
            RenderTextLayouts(textInstances.Layouts.MutateAll(),
                textInstances.ClearFlags.MutateAll(),
                textInstances.RenderPriorities.Enumerate(),
                textInstances.TransformMatrices.Enumerate());
        }

        public void RenderTextLayouts(
            Span<TextLayout> layouts,
            Span<bool> clearFlags,
            ReadOnlySpan<int> renderPriorities,
            ReadOnlySpan<Matrix4x4> transforms)
        {
            if (layouts.IsEmpty) { return; }
            GraphicsDevice device = _renderContext.Device;
            QuadBatcher batcher = _renderContext.QuadBatcher;
            CommandList cl = _renderContext.GetFreeCommandList();
            Span<byte> buffer = _nativeBuffer.AsSpan<byte>();

            for (int i = 0; i < layouts.Length; i++)
            {
                TextLayout layout = layouts[i];
                if (layout == null) { continue; }
                ref bool clear = ref clearFlags[i];
                TextureView texView = _textureViews[i];
                if (layout.DirtyGlyphs.Count == 0 && !clear) { goto present; }

                Texture staging = _stagingTextures[i];
                Texture sampled = _layoutTextures[i];
                
                if (clear)
                {
                    device.InitStagingTexture(staging);
                    clear = false;
                }
                
                if (layout.DirtyGlyphs.Count > 0)
                {
                    var map = device.Map<RgbaByte>(staging, MapMode.Write);
                    cl.Begin();
                    foreach (ushort index in layout.DirtyGlyphs)
                    {
                        DrawGlyph(layout, i, cl, index, buffer, map);
                    }

                    cl.End();
                    device.Unmap(staging);
                    device.SubmitCommands(cl);

                    layout.DirtyGlyphs.Clear();
                }

                cl.Begin();
                cl.CopyTexture(staging, sampled);
                cl.End();
                device.SubmitCommands(cl);
                
            present:
                RgbaFloat color = RgbaFloat.White;
                var dstRect = new RectangleF(0, 0, layout.MaxBounds.Width, layout.MaxBounds.Height);
                batcher.SetTransform(transforms[i]);
                batcher.DrawImage(texView, null, dstRect, ref color, renderPriorities[i]);
            }

            _renderContext.FreeCommandList(cl);
        }

        private void DrawGlyph(
            TextLayout textLayout, int textureIndex,
            CommandList commandList, uint glyphIndex,
            Span<byte> srcBuffer, MappedResourceView<RgbaByte> dstBuffer)
        {
            ref LayoutGlyph glyph = ref textLayout.Glyphs[glyphIndex];
            if (glyph.Color.A == 0)
            {
                return;
            }

            FontFace font = textLayout.FontFamily.GetFace(glyph.FontStyle);
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
            RgbaFloat color = glyph.Color;
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
                _stagingTextures[textureIndex], (uint)pos.X, (uint)pos.Y, 0, 0, 0, _layoutTextures[textureIndex],
                (uint)pos.X, (uint)pos.Y, 0, 0, 0, dimensions.Width, dimensions.Height, 1, 1);
        }

        public void Dispose()
        {
            _nativeBuffer.Dispose();
            ArrayUtil.DisposeElements(_textureViews);
        }
    }
}
