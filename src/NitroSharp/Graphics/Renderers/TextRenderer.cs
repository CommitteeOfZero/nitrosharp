using System;
using System.Numerics;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Renderers
{
    internal struct TextSystemData
    {
        public Texture StagingTexture;
        public Texture LayoutTexture;
        public TextureView TextureView;
    }

    internal sealed class TextRenderer : IDisposable
    {
        private readonly World _world;
        private readonly RenderContext _renderContext;

        private NativeMemory _nativeBuffer = NativeMemory.Allocate(256 * 256 * 4);

        public TextRenderer(World world, RenderContext renderContext)
        {
            _world = world;
            _renderContext = renderContext;
        }

        public void ProcessTextLayouts()
        {
            var textInstances = _world.TextInstances;

            RgbaTexturePool texturePool = _renderContext.TexturePool;
            var removed = textInstances.SystemData.RecycledComponents;
            foreach (TextSystemData data in removed)
            {
                if (data.LayoutTexture != null)
                {
                    texturePool.Return(data.StagingTexture);
                    texturePool.Return(data.LayoutTexture);
                    _renderContext.Device.DisposeWhenIdle(data.TextureView);
                }
            }

            var added = textInstances.AddedEntities;
            foreach (Entity entity in added)
            {
                ref TextSystemData data = ref textInstances.SystemData.Mutate(entity);
                TextLayout layout = textInstances.Layouts.GetValue(entity);
                data.StagingTexture = texturePool.RentStaging(layout.MaxBounds);
                Texture sampled = texturePool.RentSampled(layout.MaxBounds);
                data.LayoutTexture = sampled;
                data.TextureView = _renderContext.ResourceFactory.CreateTextureView(sampled);
            }

            TransformProcessor.ProcessTransforms(_world, textInstances);
            RenderTextLayouts(textInstances.Layouts.Enumerate(),
                textInstances.ClearFlags.MutateAll(),
                textInstances.SortKeys.Enumerate(),
                textInstances.TransformMatrices.Enumerate(),
                textInstances.SystemData.Enumerate());
        }

        public void RenderTextLayouts(
            ReadOnlySpan<TextLayout> layouts,
            Span<bool> clearFlags,
            ReadOnlySpan<RenderItemKey> renderPriorities,
            ReadOnlySpan<Matrix4x4> transforms,
            ReadOnlySpan<TextSystemData> systemData)
        {
            if (layouts.IsEmpty) { return; }
            GraphicsDevice device = _renderContext.Device;
            QuadBatcher batcher = _renderContext.QuadBatcher;
            CommandList cl = _renderContext.GetFreeCommandList();
            Span<byte> buffer = _nativeBuffer.AsSpan<byte>();

            for (int i = 0; i < layouts.Length; i++)
            {
                TextLayout layout = layouts[i];
                ref bool clear = ref clearFlags[i];
                ref readonly TextSystemData sd = ref systemData[i];
                TextureView texView = sd.TextureView;

                if (layout.DirtyGlyphs.Count == 0 && !clear) { goto present; }

                Texture staging = sd.StagingTexture;
                Texture sampled = sd.LayoutTexture;
                
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
                        DrawGlyph(layout, i, cl, index, buffer, map, sd);
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
                batcher.DrawImage(texView, dstRect, dstRect, ref color, renderPriorities[i]);
            }

            _renderContext.FreeCommandList(cl);
        }
        
        private void DrawGlyph(
            TextLayout textLayout, int textureIndex,
            CommandList commandList, uint glyphIndex,
            Span<byte> srcBuffer, MappedResourceView<RgbaByte> dstBuffer,
            in TextSystemData systemData)
        {
            ref LayoutGlyph glyph = ref textLayout.Glyphs[glyphIndex];
            if (glyph.Color.A == 0) { return; }

            FontFace font = textLayout.FontFamily.GetFace(glyph.FontStyle);
            font.GetGlyphInfo(glyph.Char, out GlyphInfo glyphInfo);

            GlyphBitmapInfo bitmapInfo = font.RasterizeGlyph(ref glyphInfo, srcBuffer);
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
               systemData.StagingTexture, (uint)pos.X, (uint)pos.Y, 0, 0, 0, systemData.LayoutTexture,
                (uint)pos.X, (uint)pos.Y, 0, 0, 0, dimensions.Width, dimensions.Height, 1, 1);
        }

        public void Dispose()
        {
            _nativeBuffer.Dispose();
            foreach (TextSystemData sd in _world.TextInstances.SystemData.Enumerate())
            {
                sd.TextureView?.Dispose();
            }
        }
    }
}
