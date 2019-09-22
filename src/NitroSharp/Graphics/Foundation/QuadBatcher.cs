using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal readonly struct QuadMaterial
    {
        public readonly ResourceSet ResourceSet;
        public readonly Vector2 UvTopLeft;
        public readonly Vector2 UvBottomRight;

        public QuadMaterial(ResourceSet resourceSet, Vector2 uvTopLeft, Vector2 uvBottomRight)
        {
            ResourceSet = resourceSet;
            UvTopLeft = uvTopLeft;
            UvBottomRight = uvBottomRight;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GpuQuad : GpuType
    {
        public const uint SizeInGpuBlocks = 8;

        public QuadVertex TL;
        public QuadVertex TR;
        public QuadVertex BL;
        public QuadVertex BR;
        public RgbaFloat Color;

        public unsafe void WriteGpuBlocks(Span<Vector4> blocks)
        {
            fixed (void* ptr = &TL)
            {
                var src = new Span<Vector4>(ptr, (int)SizeInGpuBlocks);
                src.CopyTo(blocks);
            }
        }
    }

    internal sealed class QuadBatcher : IDisposable
    {
        private readonly RenderBucket<RenderItemKey> _renderBucket;
        private readonly QuadGeometryStream _quadGeometryStream;
        private readonly ResourceFactory _resourceFactory;
        private readonly ViewProjection _viewProjection;
        private readonly ResourceSetCache _resourceSetCache;
        private readonly Texture _whiteTexture;
        private readonly ResourceLayout _resourceLayout0;
        private readonly ResourceLayout _spriteResourceLayout;
        private ResourceSetDescription _spriteResourceSetDesc;
        private readonly Pipeline _alphaBlendPipeline;
        private readonly Pipeline _additiveBlendPipeline;
        private readonly Pipeline _subtractiveBlendPipeline;
        private readonly Pipeline _mulpiplicativeBlendPipeline;
        private readonly VertexList<int> _instanceData;
        private ResourceSet? _lastResourceSet;
        private Texture? _lastTexture;
        private Matrix4x4 _transform;
        private ArrayBuilder<Quad> _quads;
        private ResourceSet? _resourceSet0;
        private readonly GpuCache<GpuQuad> _gpuQuads;

        private struct Quad
        {
            public RenderItemKey Key;
            public Pipeline Pipeline;
            public ResourceSet ResourceSet;
        }

        public QuadBatcher(
            GraphicsDevice graphicsDevice,
            Framebuffer framebuffer,
            ViewProjection viewProjection,
            RenderBucket<RenderItemKey> renderBucket,
            QuadGeometryStream quadGeometryStream,
            ResourceSetCache resourceSetCache,
            ShaderLibrary shaderLibrary,
            Texture whiteTexture)
        {
            _quads = new ArrayBuilder<Quad>(initialCapacity: 32);
            GraphicsDevice gd = graphicsDevice;
            _resourceFactory = gd.ResourceFactory;
            _viewProjection = viewProjection;
            _renderBucket = renderBucket;
            _quadGeometryStream = quadGeometryStream;
            _resourceSetCache = resourceSetCache;
            _whiteTexture = whiteTexture;

            _gpuQuads = new GpuCache<GpuQuad>(graphicsDevice, GpuQuad.SizeInGpuBlocks, 512);

            ResourceFactory factory = gd.ResourceFactory;

            _resourceLayout0 = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("CacheTexture", ResourceKind.TextureReadOnly, ShaderStages.Vertex)
            ));

            _spriteResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            _spriteResourceSetDesc = new ResourceSetDescription(_spriteResourceLayout, new BindableResource[2]);
            _spriteResourceSetDesc.BoundResources[0] = whiteTexture;
            _spriteResourceSetDesc.BoundResources[1] = gd.LinearSampler;

            ResourceSet solidColorResourceSet = factory.CreateResourceSet(_spriteResourceSetDesc);
            SolidColorMaterial = new QuadMaterial(solidColorResourceSet, Vector2.Zero, Vector2.One);


            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("quad");
            var shaderSetDesc = new ShaderSetDescription(
                Array.Empty<VertexLayoutDescription>(),
                new[] { vs, fs }
            );

            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSetDesc,
                new[] { viewProjection.ResourceLayout, _resourceLayout0, _spriteResourceLayout },
                framebuffer.OutputDescription
            );

            _alphaBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
            pipelineDesc.BlendState = BlendStateDescription.SingleAdditiveBlend;
            _additiveBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.BlendState = new BlendStateDescription
            {
                AttachmentStates = new BlendAttachmentDescription[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.One,
                        DestinationColorFactor = BlendFactor.One,
                        ColorFunction = BlendFunction.Subtract,
                        SourceAlphaFactor = BlendFactor.One,
                        DestinationAlphaFactor = BlendFactor.One,
                        AlphaFunction = BlendFunction.Subtract
                    }
                }
            };
            _subtractiveBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.BlendState = new BlendStateDescription
            {
                AttachmentStates = new BlendAttachmentDescription[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.Zero,
                        DestinationColorFactor = BlendFactor.SourceColor,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = BlendFactor.Zero,
                        DestinationAlphaFactor = BlendFactor.SourceAlpha,
                        AlphaFunction = BlendFunction.Add
                    }
                }
            };
            _mulpiplicativeBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

            _instanceData = new VertexList<int>(graphicsDevice, initialCapacity: 64);
        }

        public QuadMaterial SolidColorMaterial { get; }

        public void BeginFrame()
        {
            //_instanceData.Begin();
            _gpuQuads.BeginFrame(clear: true);
            _quads.Clear();
        }

        public void EndFrame(CommandList cl)
        {
            //_instanceData.End(cl);
            _gpuQuads.EndFrame(cl);

            ResourceSet? rs0 = _resourceSet0;
            Texture cacheTexture = _gpuQuads.GetCacheTexture(out bool realloc);
            if (rs0 == null || realloc)
            {
                _resourceSet0 = _resourceFactory.CreateResourceSet(
                    new ResourceSetDescription(_resourceLayout0, cacheTexture)
                );
            }

            var multiSub = _renderBucket.PrepareMultiSubmission(_quads.Count);
            for (int i = 0; i < _quads.Count; i++)
            {
                ref Quad q = ref _quads[i];
                multiSub.Keys[i] = q.Key;
                multiSub.Submissions[i] = new RenderBucketSubmission
                {
                    VertexBase = (ushort)(i * 6),
                    VertexCount = 6,
                    Pipeline = q.Pipeline,
                    SharedResourceSet = _viewProjection.ResourceSet,
                    ObjectResourceSet0 = _resourceSet0!,
                    ObjectResourceSet1 = q.ResourceSet,
                    InstanceCount = 1
                };
            }
            _renderBucket.Submit(multiSub);
        }

        public QuadMaterial CreateMaterial(Texture texture, in RectangleF? sourceRect)
        {
            var dimensions = new Vector2(texture.Width, texture.Height);
            var sourceRectangle = sourceRect ?? new RectangleF(0, 0, dimensions.X, dimensions.Y);
            var srcTopLeft = new Vector2(sourceRectangle.Left, sourceRectangle.Top);
            var srcBottomRight = new Vector2(sourceRectangle.Right, sourceRectangle.Bottom);
            Vector2 uvTopLeft = srcTopLeft / dimensions;
            Vector2 uvBottomRight = srcBottomRight / dimensions;

            _spriteResourceSetDesc.BoundResources[0] = texture;
            ResourceSet set = _resourceSetCache.GetResourceSet(ref _spriteResourceSetDesc);
            return new QuadMaterial(set, uvTopLeft, uvBottomRight);
        }

        public void SetTransform(in Matrix4x4 transform)
        {
            _transform = transform;
        }

        public void FillRectangle(
            float x,
            float y,
            float width,
            float height,
            ref RgbaFloat fillColor,
            RenderItemKey renderPriority)
            => FillRectangle(new RectangleF(x, y, width, height), ref fillColor, renderPriority);

        public void FillRectangle(
            in RectangleF rect,
            ref RgbaFloat fillColor,
            RenderItemKey renderPriority)
            => DrawImage(_whiteTexture, null, rect, ref fillColor, renderPriority);

        public void BatchQuads(
            ReadOnlySpan<CommonItemProperties> ribs,
            ReadOnlySpan<SizeF> dstSizes,
            QuadMaterial material,
            ReadOnlySpan<Matrix4x4> transforms)
            => BatchQuads(ribs, dstSizes, MemoryMarshal.CreateSpan(ref material, 1), transforms);

        public void BatchQuads(
            ReadOnlySpan<CommonItemProperties> ribs,
            ReadOnlySpan<SizeF> dstSizes,
            ReadOnlySpan<QuadMaterial> materials,
            ReadOnlySpan<Matrix4x4> transforms)
        {
            QuadGeometryStream geometryStream = _quadGeometryStream;
            VertexList<QuadInstanceData> instanceDataBuffer = geometryStream.InstanceDataBuffer;
            VertexList<QuadVertex> vertexBuffer = geometryStream.VertexBuffer;
            ResourceSet commonResourceSet = _viewProjection.ResourceSet;

            int count = ribs.Length;
            uint vertexBase = vertexBuffer.Count; 
            uint instanceBase = instanceDataBuffer.Count;
            //Span<QuadInstanceData> instanceData = instanceDataBuffer.Append((uint)count);
            //Span<int> instanceData = _instanceData.Append((uint)count);
            var gpuQuad = new GpuQuad();
            for (int i = 0; i < count; i++)
            {
                //ref QuadInstanceData data = ref instanceData[i];
                // Covers the case when there's only 1 material used for all quads
                int materialIndex = materials.Length > 1 ? i : 0;
                QuadMaterial material = materials[materialIndex];
                Matrix4x4 transform = transforms[i];
                SizeF dstSize = dstSizes[i];
                CommonItemProperties commonProprs = ribs[i];
                //data.Color = commonProprs.Color;

                ref var VertexTL = ref gpuQuad.TL;
                VertexTL.Position.X = 0.0f;
                VertexTL.Position.Y = 0.0f;
                VertexTL.TexCoord.X = material.UvTopLeft.X;
                VertexTL.TexCoord.Y = material.UvTopLeft.Y;
                VertexTL.Position = Vector2.Transform(VertexTL.Position, transform);

                ref var VertexTR = ref gpuQuad.TR;
                VertexTR.Position.X = dstSize.Width;
                VertexTR.Position.Y = 0.0f;
                VertexTR.TexCoord.X = material.UvBottomRight.X;
                VertexTR.TexCoord.Y = material.UvTopLeft.Y;
                VertexTR.Position = Vector2.Transform(VertexTR.Position, transform);

                ref var VertexBL = ref gpuQuad.BL;
                VertexBL.Position.X = 0.0f;
                VertexBL.Position.Y = 0.0f + dstSize.Height;
                VertexBL.TexCoord.X = material.UvTopLeft.X;
                VertexBL.TexCoord.Y = material.UvBottomRight.Y;
                VertexBL.Position = Vector2.Transform(VertexBL.Position, transform);

                ref var VertexBR = ref gpuQuad.BR;
                VertexBR.Position.X = dstSize.Width;
                VertexBR.Position.Y = dstSize.Height;
                VertexBR.TexCoord.X = material.UvBottomRight.X;
                VertexBR.TexCoord.Y = material.UvBottomRight.Y;
                VertexBR.Position = Vector2.Transform(VertexBR.Position, transform);

                gpuQuad.Color = commonProprs.Color;
                _gpuQuads.Insert(ref gpuQuad);

                //instanceData[i] = _colors.GetCachePosition(tlHandle);

            }

            //var multiSub = _renderBucket.PrepareMultiSubmission((uint)count);
            for (int i = 0; i < count; i++)
            {
                CommonItemProperties commonProps = ribs[i];
                int materialIndex = materials.Length > 1 ? i : 0;
                Pipeline pipeline = commonProps.BlendMode switch
                {
                    BlendMode.Alpha => _alphaBlendPipeline,
                    BlendMode.Additive => _additiveBlendPipeline,
                    BlendMode.Subtractive => _subtractiveBlendPipeline,
                    BlendMode.Multiplicative => _mulpiplicativeBlendPipeline,
                    _ => ThrowHelper.UnexpectedValue<Pipeline>()
                };

                //multiSub.Keys[i] = commonProps.Key;
                //multiSub.Submissions[i] = new RenderBucketSubmission
                //{
                //    VertexBuffer0 = _instanceData,
                //    VertexCount = 6,
                //    InstanceBase = (ushort)(instanceBase + i),
                //    Pipeline = pipeline,
                //    SharedResourceSet = commonResourceSet,
                //    ObjectResourceSet0 = materials[materialIndex].ResourceSet,
                //    InstanceCount = 1
                //};

                ref Quad q = ref _quads.Add();
                q.Key = commonProps.Key;
                q.Pipeline = pipeline;
                q.ResourceSet = materials[materialIndex].ResourceSet;
            }

            //_renderBucket.Submit(multiSub);
        }

        public void DrawImage(
            Texture texture,
            in RectangleF? sourceRect,
            in RectangleF destinationRect,
            ref RgbaFloat color,
            RenderItemKey renderPriority,
            BlendMode blendMode = BlendMode.Alpha)
        {
            (uint width, uint height) = (texture.Width, texture.Height);
            var sourceRectangle = sourceRect ?? new RectangleF(0, 0, width, height);

            var uvTopLeft = new Vector2(
                sourceRectangle.Left / width,
                sourceRectangle.Top / height);

            var uvBottomRight = new Vector2(
                sourceRectangle.Right / width,
                sourceRectangle.Bottom / height);

            (ushort vertexBase, ushort instanceBase) = _quadGeometryStream.Append(
                destinationRect, uvTopLeft, uvBottomRight, _transform, ref color);

            ResourceSet? resourceSet = _lastResourceSet;
            if (texture != _lastTexture)
            {
                _spriteResourceSetDesc.BoundResources[0] = texture;
                resourceSet = _resourceSetCache.GetResourceSet(ref _spriteResourceSetDesc);
                _lastTexture = texture;
                _lastResourceSet = resourceSet;
            }

            Pipeline pipeline = blendMode switch
            {
                BlendMode.Alpha => _alphaBlendPipeline,
                BlendMode.Additive => _additiveBlendPipeline,
                BlendMode.Subtractive => _subtractiveBlendPipeline,
                BlendMode.Multiplicative => _mulpiplicativeBlendPipeline,
                _ => ThrowHelper.UnexpectedValue<Pipeline>()
            };

            Debug.Assert(resourceSet != null);
            var submission = new RenderBucketSubmission
            {
                VertexBuffer0 = _quadGeometryStream.InstanceDataBuffer,
                //IndexBuffer = _quadGeometryStream.IndexBuffer,
                VertexBase = vertexBase,
                //IndexBase = 0,
                //IndexCount = 6,
                VertexCount = 6,
                //VertexBuffer0 = _quadGeometryStream.InstanceDataBuffer,
                InstanceBase = instanceBase,
                Pipeline = pipeline,
                SharedResourceSet = _viewProjection.ResourceSet,
                ObjectResourceSet0 = resourceSet,
                InstanceCount = 1
            };

            _renderBucket.Submit<QuadVertex, QuadInstanceData>(ref submission, renderPriority);
        }

        public void Dispose()
        {
            _alphaBlendPipeline.Dispose();
            _additiveBlendPipeline.Dispose();
            _subtractiveBlendPipeline.Dispose();
            _mulpiplicativeBlendPipeline.Dispose();
            _spriteResourceLayout.Dispose();
        }
    }
}
