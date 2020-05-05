using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using NitroSharp.Graphics.Core;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal struct GpuGlyph
    {
        public Vector2 Offset;
        public int GlyphRunId;
        public int GlyphId;
        public int OutlineId;
        public float Opacity;

        public static VertexLayoutDescription LayoutDescription => new VertexLayoutDescription(
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

    internal sealed class TextRenderContext : IDisposable
    {
        internal readonly struct GpuGlyphRun : GpuType
        {
            public const uint SizeInGpuBlocks = 2;

            public readonly RgbaFloat Color;
            public readonly RgbaFloat OutlineColor;

            public GpuGlyphRun(in RgbaFloat color, in RgbaFloat outlineColor)
            {
                Color = color;
                OutlineColor = outlineColor;
            }

            public void WriteGpuBlocks(Span<Vector4> blocks)
            {
                blocks[0] = Color.ToVector4();
                blocks[1] = OutlineColor.ToVector4();
            }
        }

        internal readonly struct GpuTransform : GpuType
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

        private readonly GlyphRasterizer _glyphRasterizer;
        private readonly TextureCache _textureCache;
        private readonly GpuList<GpuGlyph> _gpuGlyphs;
        private readonly GpuCache<GpuGlyphRun> _gpuGlyphRuns;
        private readonly GpuCache<GpuTransform> _gpuTransforms;

        public TextRenderContext(
            GraphicsDevice gd,
            GlyphRasterizer glyphRasterizer,
            TextureCache textureCache)
        {
            _glyphRasterizer = glyphRasterizer;
            _textureCache = textureCache;
            _gpuGlyphs = new GpuList<GpuGlyph>(
                 gd,
                 BufferUsage.VertexBuffer,
                 initialCapacity: 2048
             );
             _gpuGlyphRuns = new GpuCache<GpuGlyphRun>(
                gd,
                GpuGlyphRun.SizeInGpuBlocks,
                dimension: 128
            );
            _gpuTransforms = new GpuCache<GpuTransform>(
                gd,
                GpuTransform.SizeInGpuBlocks,
                dimension: 128
            );
        }

        public void BeginFrame()
        {
            _gpuGlyphRuns.BeginFrame(clear: true);
            _gpuTransforms.BeginFrame(clear: true);
            _gpuGlyphs.Begin();
            ResolveGlyphs();
        }

        public void RequestGlyphs(TextLayout textLayout)
        {
            foreach (ref readonly GlyphRun glyphRun in textLayout.GlyphRuns)
            {
                ReadOnlySpan<PositionedGlyph> glyphs = textLayout.GetGlyphs(glyphRun.GlyphSpan);
                _glyphRasterizer.RequestGlyphs(glyphRun.Font, glyphRun.FontSize, glyphs, _textureCache);
            }
        }

        public void Render(RenderContext ctx, DrawBatch drawBatch, TextLayout layout, in Matrix4x4 transform)
        {
            TextShaderResources shaderResources = ctx.ShaderResources.Text;
            foreach (ref readonly GlyphRun glyphRun in layout.GlyphRuns)
            {
                ReadOnlySpan<PositionedGlyph> glyphs = layout.GetGlyphs(glyphRun.GlyphSpan);
                if (AppendRun(glyphRun, glyphs, transform) is GpuGlyphSlice gpuGlyphSlice)
                {
                    drawBatch.PushDraw(new Draw
                    {
                        Pipeline = shaderResources.Pipeline,
                        BufferBindings = new BufferBindings(gpuGlyphSlice.Buffer),
                        ResourceBindings = new ResourceBindings(
                            new ResourceSetKey(
                                shaderResources.ResourceLayoutVS,
                                drawBatch.Target.ViewProjection.Buffer.VdBuffer,
                                _gpuGlyphRuns.Texture,
                                _gpuTransforms.Texture,
                                _textureCache.UvRectTexture
                            ),
                            new ResourceSetKey(
                                shaderResources.ResourceLayoutFS,
                                _textureCache.GetCacheTexture(PixelFormat.R8_UNorm, out _),
                                ctx.GraphicsDevice.PointSampler
                            )
                        ),
                        Params = DrawParams.Regular(
                            vertexBase: 0,
                            vertexCount: 6,
                            gpuGlyphSlice.InstanceBase,
                            gpuGlyphSlice.InstanceCount
                        )
                    });
                }
            }
        }

        public void ResolveGlyphs()
        {
            ValueTask vt = _glyphRasterizer.ResolveGlyphs(_textureCache);
            if (!vt.IsCompleted)
            {
                vt.GetAwaiter().GetResult();
            }
        }

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

        private GpuGlyphSlice? AppendRun(
            in GlyphRun run,
            ReadOnlySpan<PositionedGlyph> positionedGlyphs,
            in Matrix4x4 matrix)
        {
            var gpuGlyphRun = new GpuGlyphRun(run.Color, run.OutlineColor);
            GpuCacheHandle glyphRunHandle = _gpuGlyphRuns.Insert(ref gpuGlyphRun);
            int glyphRunId = _gpuGlyphRuns.GetCachePosition(glyphRunHandle);

            var transform = new GpuTransform(matrix);
            GpuCacheHandle transformHandle = _gpuTransforms.Insert(ref transform);
            Debug.Assert(_gpuTransforms.GetCachePosition(transformHandle) == glyphRunId);

            FontData fontData = _glyphRasterizer.GetFontData(run.Font);
            uint instanceBase = _gpuGlyphs.Count;
            DeviceBuffer? buffer = null;
            foreach (PositionedGlyph glyph in positionedGlyphs)
            {
                var key = new GlyphCacheKey(glyph.Index, run.FontSize);
                if (fontData.TryGetCachedGlyph(key, out GlyphCacheEntry cachedGlyph)
                    && cachedGlyph.IsRegular)
                {
                    TextureCacheItem glyphTci = _textureCache.Get(cachedGlyph.TextureCacheHandle);
                    int outlineId = 0;
                    if (cachedGlyph.OutlineTextureCacheHandle.IsValid)
                    {
                        TextureCacheItem outlineTci = _textureCache
                            .Get(cachedGlyph.OutlineTextureCacheHandle);
                        outlineId = outlineTci.UvRectPosition;
                    }
                    (buffer, _) = _gpuGlyphs.Append(new GpuGlyph
                    {
                        Offset = glyph.Position,
                        GlyphRunId = glyphRunId,
                        GlyphId = glyphTci.UvRectPosition,
                        OutlineId = outlineId,
                        Opacity = 1.0f
                    });
                }
            }

            return buffer is null
                ? (GpuGlyphSlice?)null
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
}
