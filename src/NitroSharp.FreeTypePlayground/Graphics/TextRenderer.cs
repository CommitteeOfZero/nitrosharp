using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using NitroSharp.Graphics;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class TextRenderer : IDisposable
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

        private readonly GraphicsDevice _graphicsDevice;
        private readonly GlyphRasterizer _glyphRasterizer;
        private readonly DeviceBuffer _projectionBuffer;
        private readonly Pipeline _pipeline;
        private readonly Pipeline _outlinePipeline;
        private readonly TextureCache _textureCache;
        private readonly VertexList<GpuGlyph> _gpuGlyphs;
        private readonly ResourceLayout _vsLayout;
        private readonly ResourceLayout _fsLayout;
        private ResourceSet? _vsResourceSet;
        private ResourceSet? _fsResourceSet;
        private ResourceSet? _fsOutlineResourceSet;

        private readonly GpuCache<GpuGlyphRun> _gpuGlyphRuns;
        private readonly GpuCache<GpuTransform> _gpuTransforms;

        public TextRenderer(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            GlyphRasterizer glyphRasterizer,
            TextureCache textureCache,
            DeviceBuffer projectionBuffer)
        {
            _graphicsDevice = graphicsDevice;
            _glyphRasterizer = glyphRasterizer;
            _textureCache = textureCache;
            _gpuGlyphs = new VertexList<GpuGlyph>(graphicsDevice, initialCapacity: 512);
            _projectionBuffer = projectionBuffer;
            _gpuGlyphRuns = new GpuCache<GpuGlyphRun>(
                graphicsDevice,
                GpuGlyphRun.SizeInGpuBlocks,
                initialTextureDimension: 32
            );
            _gpuTransforms = new GpuCache<GpuTransform>(
                graphicsDevice,
                GpuTransform.SizeInGpuBlocks,
                initialTextureDimension: 32
            );

            ResourceFactory factory = graphicsDevice.ResourceFactory;
            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("text");
            (Shader outlineVS, Shader outlineFS) = shaderLibrary.GetShaderSet("outline");

            _vsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "GlyphRuns",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "Transforms",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "GlyphRects",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                )
            ));

            _fsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "CacheTexture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            var vertexLayouts = new VertexLayoutDescription[]
            {
                GpuGlyph.LayoutDescription
            };
            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    vertexLayouts,
                    new[] { vs, fs }
                ),
                new[] { _vsLayout, _fsLayout },
                graphicsDevice.SwapchainFramebuffer.OutputDescription
            );
            _pipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.ShaderSet = new ShaderSetDescription(
                vertexLayouts,
                new[] { outlineVS, outlineFS }
            );
            _outlinePipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
        }

        public void BeginFrame()
        {
            _gpuGlyphRuns.BeginFrame(clear: true);
            _gpuTransforms.BeginFrame(clear: true);
            _gpuGlyphs.Begin();
        }

        public void RequestGlyphs(TextLayout textLayout)
        {
            foreach (ref readonly GlyphRun glyphRun in textLayout.GlyphRuns)
            {
                ReadOnlySpan<PositionedGlyph> glyphs = textLayout.GetGlyphs(glyphRun.GlyphSpan);
                _glyphRasterizer.RequestGlyphs(glyphRun.Font, glyphRun.FontSize, glyphs, _textureCache);
            }
        }

        public void ResolveGlyphs(TextLayout textLayout)
        {
            ValueTask vt = _glyphRasterizer.ResolveGlyphs(_textureCache);
            if (!vt.IsCompleted)
            {
                vt.GetAwaiter().GetResult();
            }
            foreach (ref readonly GlyphRun glyphRun in textLayout.GlyphRuns)
            {
                ReadOnlySpan<PositionedGlyph> glyphs = textLayout.GetGlyphs(glyphRun.GlyphSpan);
                AppendRun(glyphRun, glyphs);
            }
        }

        private void AppendRun(in GlyphRun glyphRun, ReadOnlySpan<PositionedGlyph> glyphs)
        {
            var gpuGlyphRun = new GpuGlyphRun(glyphRun.Color, glyphRun.OutlineColor);
            GpuCacheHandle glyphRunHandle = _gpuGlyphRuns.Insert(ref gpuGlyphRun);
            int glyphRunId = _gpuGlyphRuns.GetCachePosition(glyphRunHandle);

            var transform = new GpuTransform(Matrix4x4.Identity);
            GpuCacheHandle transformHandle = _gpuTransforms.Insert(ref transform);
            Debug.Assert(_gpuTransforms.GetCachePosition(transformHandle) == glyphRunId);

            FontData fontData = _glyphRasterizer.GetFontData(glyphRun.Font);
            foreach (PositionedGlyph glyph in glyphs)
            {
                var key = new GlyphCacheKey(glyph.Index, glyphRun.FontSize);
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
                    _gpuGlyphs.Append(new GpuGlyph
                    {
                        Offset = glyph.Position,
                        GlyphRunId = glyphRunId,
                        GlyphId = glyphTci.UvRectPosition,
                        OutlineId = outlineId,
                        Opacity = 1.0f
                    });
                }
            }
        }

        public void EndFrame(RenderBucket<int> renderBucket, CommandList commandList)
        {
            void updateResourceSet(ref ResourceSet? fsResourceSet, PixelFormat pixelFormat)
            {
                ResourceFactory factory = _graphicsDevice.ResourceFactory;
                Texture glyphTex = _textureCache.GetCacheTexture(pixelFormat, out bool realloc);
                if (fsResourceSet == null || realloc)
                {
                    fsResourceSet?.Dispose();
                    fsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                        _fsLayout, glyphTex, _graphicsDevice.PointSampler
                    ));
                }
            }

            _gpuGlyphRuns.EndFrame(commandList);
            _gpuTransforms.EndFrame(commandList);
            _gpuGlyphs.End(commandList);

            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            Texture uvRectTex = _textureCache.GetUvRectCacheTexture(out bool uvRectTexReallocated);
            Texture glyphRunTex = _gpuGlyphRuns.GetCacheTexture(out bool runTexReallocated);
            Texture transformTex = _gpuTransforms.GetCacheTexture(out bool transformTexReallocated);
            if (_vsResourceSet == null || uvRectTexReallocated
                || runTexReallocated || transformTexReallocated)
            {
                _vsResourceSet?.Dispose();
                _vsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                    _vsLayout, _projectionBuffer, glyphRunTex, transformTex, uvRectTex
                ));
            }

            updateResourceSet(ref _fsResourceSet, PixelFormat.R8_UNorm);
            updateResourceSet(ref _fsOutlineResourceSet, PixelFormat.R8_G8_B8_A8_UNorm);
            Debug.Assert(_vsResourceSet != null);
            Debug.Assert(_fsResourceSet != null);
            Debug.Assert(_fsOutlineResourceSet != null);

            var submission = new RenderBucketSubmission<GpuGlyph>
            {
                Pipeline = _pipeline,
                SharedResourceSet = _vsResourceSet,
                ObjectResourceSet = _fsResourceSet,
                VertexBuffer = _gpuGlyphs,
                VertexCount = 6,
                InstanceBase = 0,
                InstanceCount = (ushort)_gpuGlyphs.Count
            };
            renderBucket.Submit(ref submission, 1);

            submission.Pipeline = _outlinePipeline;
            submission.ObjectResourceSet = _fsOutlineResourceSet;
            renderBucket.Submit(ref submission, 0);
        }

        public void Dispose()
        {
            _gpuGlyphRuns.Dispose();
            _gpuTransforms.Dispose();
            _gpuGlyphs.Dispose();
            _pipeline.Dispose();
            _outlinePipeline.Dispose();
            _vsLayout.Dispose();
            _fsLayout.Dispose();
            _vsResourceSet?.Dispose();
            _fsResourceSet?.Dispose();
        }

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
    }
}
