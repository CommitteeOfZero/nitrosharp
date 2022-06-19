using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NitroSharp.Graphics.Core;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class TextRenderContext : IDisposable
    {
        private readonly struct GpuGlyphSlice
        {
            public readonly DeviceBuffer Buffer;
            public readonly uint InstanceBase;
            public readonly uint InstanceCount;

            public GpuGlyphSlice(DeviceBuffer buffer, uint instanceBase, uint instanceCount)
            {
                Buffer = buffer;
                InstanceBase = instanceBase;
                InstanceCount = instanceCount;
            }
        }

        private readonly struct GpuGlyphRun : GpuType
        {
            public const uint SizeInGpuBlocks = 2;

            private readonly RgbaFloat _color;
            private readonly RgbaFloat _outlineColor;

            public GpuGlyphRun(in RgbaFloat color, in RgbaFloat outlineColor)
            {
                _color = color;
                _outlineColor = outlineColor;
            }

            public void WriteGpuBlocks(Span<Vector4> blocks)
            {
                blocks[0] = _color.ToVector4();
                blocks[1] = _outlineColor.ToVector4();
            }
        }

        private readonly struct GpuTransform : GpuType
        {
            public const uint SizeInGpuBlocks = 4;

            public readonly Matrix4x4 Transform;

            public GpuTransform(in Matrix4x4 transform)
                => Transform = transform;

            public void WriteGpuBlocks(Span<Vector4> blocks)
            {
                ref readonly Matrix4x4 tr = ref Transform;
                blocks[0] = new Vector4(tr.M11, tr.M12, tr.M13, tr.M14);
                blocks[1] = new Vector4(tr.M21, tr.M22, tr.M23, tr.M24);
                blocks[2] = new Vector4(tr.M31, tr.M32, tr.M33, tr.M34);
                blocks[3] = new Vector4(tr.M41, tr.M42, tr.M43, tr.M44);
            }
        }

        private readonly TextureCache _textureCache;
        private readonly GpuList<GpuGlyph> _gpuGlyphs;
        private readonly GpuCache<GpuGlyphRun> _gpuGlyphRuns;
        private readonly GpuCache<GpuTransform> _gpuTransforms;

        private ArrayBuilder<(Draw, int)> _pendingDraws;

        public TextRenderContext(
            GraphicsDevice gd,
            GlyphRasterizer glyphRasterizer,
            TextureCache textureCache)
        {
            GlyphRasterizer = glyphRasterizer;
            _textureCache = textureCache;
            _gpuGlyphs = new GpuList<GpuGlyph>(gd, BufferUsage.VertexBuffer, initialCapacity: 2048);
            _gpuGlyphRuns = new GpuCache<GpuGlyphRun>(gd, GpuGlyphRun.SizeInGpuBlocks, dimension: 128);
            _gpuTransforms = new GpuCache<GpuTransform>(gd, GpuTransform.SizeInGpuBlocks, dimension: 128);
            _pendingDraws = new ArrayBuilder<(Draw, int)>(4);
        }

        public GlyphRasterizer GlyphRasterizer { get; }

        public void BeginFrame()
        {
            _gpuGlyphRuns.BeginFrame(clear: true);
            _gpuTransforms.BeginFrame(clear: true);
            _gpuGlyphs.Begin();
        }

        public void RequestGlyphs(TextLayout textLayout)
        {
            RequestGlyphs(textLayout, textLayout.GlyphRuns);
        }

        public void RequestGlyphs(TextLayout textLayout, GlyphRun glyphRun)
        {
            RequestGlyphs(textLayout, MemoryMarshal.CreateReadOnlySpan(ref glyphRun, 1));
        }

        private void RequestGlyphs(TextLayout textLayout, ReadOnlySpan<GlyphRun> glyphRuns)
        {
            foreach (ref readonly GlyphRun glyphRun in glyphRuns)
            {
                ReadOnlySpan<PositionedGlyph> glyphs = textLayout.Glyphs[glyphRun.GlyphSpan];
                GlyphRasterizer.RequestGlyphs(
                    glyphRun.Font,
                    glyphRun.FontSize,
                    glyphs,
                    _textureCache,
                    glyphRun.DrawOutline
                );
            }
        }

        public void Render(
            RenderContext ctx,
            DrawBatch drawBatch,
            TextLayout layout,
            in Matrix4x4 transform,
            Vector2 offset,
            in RectangleU rect,
            float opacity)
        {
            Render(ctx, drawBatch, layout, layout.GlyphRuns, transform, offset, rect, opacity);
        }

        public void Render(
           RenderContext ctx,
           DrawBatch drawBatch,
           TextLayout layout,
           GlyphRun glyphRun,
           in Matrix4x4 transform,
           Vector2 offset,
           in RectangleU rect,
           float opacity)
        {
            var span = MemoryMarshal.CreateReadOnlySpan(ref glyphRun, 1);
            Render(ctx, drawBatch, layout, span, transform, offset, rect, opacity);
        }

        private void Render(
            RenderContext ctx,
            DrawBatch drawBatch,
            TextLayout layout,
            ReadOnlySpan<GlyphRun> glyphRuns,
            in Matrix4x4 transform,
            Vector2 offset,
            in RectangleU rect,
            float opacity)
        {
            Matrix4x4 finalTransform = Matrix4x4.CreateTranslation(new Vector3(offset, 0)) * transform;
            TextShaderResources shaderResources = ctx.ShaderResources.Text;
            for (int i = 0; i < glyphRuns.Length; i++)
            {
                ref readonly GlyphRun glyphRun = ref glyphRuns[i];
                ReadOnlySpan<PositionedGlyph> glyphs = layout.Glyphs[glyphRun.GlyphSpan];
                ReadOnlySpan<float> opacityValues = layout.GetOpacityValues(glyphRun.GlyphSpan);
                if (AppendRun(glyphRun, glyphs, opacityValues, finalTransform, opacity) is { } gpuGlyphSlice)
                {
                    _pendingDraws.Add() = (new Draw
                    {
                        Pipeline = shaderResources.Pipeline,
                        BufferBindings = new BufferBindings(gpuGlyphSlice.Buffer),
                        ResourceBindings = new ResourceBindings(
                            new ResourceSetKey(
                                shaderResources.ResourceLayoutVS,
                                ctx.OrthoProjection.Buffer.VdBuffer,
                                _gpuGlyphRuns.Texture,
                                _gpuTransforms.Texture,
                                _textureCache.UvRectTexture
                            ),
                            new ResourceSetKey(
                                shaderResources.ResourceLayoutFS,
                                _textureCache.GetCacheTexture(PixelFormat.R8_UNorm, out _),
                                ctx.GraphicsDevice.LinearSampler
                            )
                        ),
                        Params = DrawParams.Regular(
                            vertexBase: 0,
                            vertexCount: 6,
                            gpuGlyphSlice.InstanceBase,
                            gpuGlyphSlice.InstanceCount
                        ),
                        ScissorRect = rect
                    }, i);
                }
            }

            foreach (ref (Draw draw, int i) entry in _pendingDraws.AsSpan())
            {
                if (glyphRuns[entry.i].DrawOutline)
                {
                    Draw outlineDraw = entry.draw;
                    outlineDraw.ResourceBindings = new ResourceBindings(
                        new ResourceSetKey(
                            shaderResources.ResourceLayoutVS,
                            ctx.OrthoProjection.Buffer.VdBuffer,
                            _gpuGlyphRuns.Texture,
                            _gpuTransforms.Texture,
                            _textureCache.UvRectTexture
                        ),
                        new ResourceSetKey(
                            shaderResources.ResourceLayoutFS,
                            _textureCache.GetCacheTexture(PixelFormat.R8_G8_B8_A8_UNorm, out _),
                            ctx.GraphicsDevice.LinearSampler
                        )
                    );
                    outlineDraw.Pipeline = ctx.ShaderResources.Text.OutlinePipeline;
                    drawBatch.PushDraw(outlineDraw);
                }
            }

            foreach (ref (Draw draw, int i) entry in _pendingDraws.AsSpan())
            {
                drawBatch.PushDraw(entry.draw);
            }

            _pendingDraws.Clear();
        }

        public void ResolveGlyphs()
        {
            ValueTask vt = GlyphRasterizer.ResolveGlyphs(_textureCache);
            if (!vt.IsCompleted)
            {
                vt.GetAwaiter().GetResult();
            }
        }

        private GpuGlyphSlice? AppendRun(
            in GlyphRun run,
            ReadOnlySpan<PositionedGlyph> positionedGlyphs,
            ReadOnlySpan<float> opacityValues,
            in Matrix4x4 transform,
            float opacityMul)
        {
            var gpuGlyphRun = new GpuGlyphRun(run.Color, run.OutlineColor);
            GpuCacheHandle glyphRunHandle = _gpuGlyphRuns.Insert(ref gpuGlyphRun);
            int glyphRunId = _gpuGlyphRuns.GetCachePosition(glyphRunHandle);

            var gpuTransform = new GpuTransform(transform);
            GpuCacheHandle transformHandle = _gpuTransforms.Insert(ref gpuTransform);
            Debug.Assert(_gpuTransforms.GetCachePosition(transformHandle) == glyphRunId);

            FontData fontData = GlyphRasterizer.GetFontData(run.Font);
            uint instanceBase = _gpuGlyphs.Count;
            DeviceBuffer? buffer = null;
            for (int i = 0; i < positionedGlyphs.Length; i++)
            {
                PositionedGlyph glyph = positionedGlyphs[i];
                var key = new GlyphCacheKey(glyph.Index, run.FontSize);
                if (fontData.TryGetCachedGlyph(key, out GlyphCacheEntry cachedGlyph))
                {
                    Debug.Assert(cachedGlyph.Kind != GlyphCacheEntryKind.Pending);
                    if (cachedGlyph.Kind == GlyphCacheEntryKind.Blank) { continue; }
                    TextureCacheItem glyphTci = _textureCache.Get(cachedGlyph.TextureCacheHandle);
                    int outlineId = 0;
                    if (cachedGlyph.OutlineTextureCacheHandle.IsValid)
                    {
                        TextureCacheItem outlineTci = _textureCache.Get(cachedGlyph.OutlineTextureCacheHandle);
                        outlineId = outlineTci.UvRectPosition;
                    }

                    (buffer, _) = _gpuGlyphs.Append(new GpuGlyph
                    {
                        Offset = glyph.Position,
                        GlyphRunId = glyphRunId,
                        GlyphId = glyphTci.UvRectPosition,
                        OutlineId = outlineId,
                        Opacity = opacityValues[i] * opacityMul
                    });
                }
            }

            return buffer is null
                ? null
                : new GpuGlyphSlice(buffer, instanceBase, _gpuGlyphs.Count - instanceBase);
        }

        public void EndFrame(CommandList cl)
        {
            _gpuGlyphRuns.EndFrame(cl);
            _gpuTransforms.EndFrame(cl);
            _gpuGlyphs.End(cl);
        }

        public void Dispose()
        {
            _gpuGlyphs.Dispose();
            _gpuGlyphRuns.Dispose();
            _gpuTransforms.Dispose();
        }
    }

    internal struct GpuGlyph
    {
        public Vector2 Offset;
        public int GlyphRunId;
        public int GlyphId;
        public int OutlineId;
        public float Opacity;

        public static VertexLayoutDescription LayoutDescription => new(
            stride: 24, instanceStepRate: 1,
            new VertexElementDescription(
                "vs_Offset",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_GlyphRunID",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Int1
            ),
            new VertexElementDescription(
                "vs_GlyphID",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Int1
            ),
            new VertexElementDescription(
                "vs_OutlineID",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Int1
            ),
            new VertexElementDescription(
                "vs_Opacity",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float1
            )
        );
    }
}
