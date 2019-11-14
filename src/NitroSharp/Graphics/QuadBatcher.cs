using System;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Primitives;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{

    internal struct QuadVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public Vector4 Color;

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_TexCoord",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2
            ),
            new VertexElementDescription(
                "vs_Color",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4
            )
        );
    }

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

    internal sealed class QuadBatcher : IDisposable
    {
        private readonly RenderBucket<RenderItemKey> _renderBucket;
        private readonly GraphicsDevice _gd;
        private readonly ResourceFactory _resourceFactory;
        private readonly ViewProjection _viewProjection;
        private readonly ResourceSetCache _resourceSetCache;
        private readonly Texture _whiteTexture;

        private readonly ResourceLayout _spriteResourceLayout;
        private ResourceSetDescription _spriteResourceSetDesc;

        private readonly Pipeline _alphaBlendPipeline;
        private readonly Pipeline _additiveBlendPipeline;
        private readonly Pipeline _subtractiveBlendPipeline;
        private readonly Pipeline _mulpiplicativeBlendPipeline;
        private readonly Pipeline _grayscalePipeline;

        private readonly VertexList<QuadVertex> _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        public QuadBatcher(
            GraphicsDevice graphicsDevice,
            Framebuffer framebuffer,
            ViewProjection viewProjection,
            RenderBucket<RenderItemKey> renderBucket,
            ResourceSetCache resourceSetCache,
            ShaderLibrary shaderLibrary,
            Texture whiteTexture)
        {
            GraphicsDevice gd = graphicsDevice;
            _gd = graphicsDevice;
            _resourceFactory = gd.ResourceFactory;
            _viewProjection = viewProjection;
            _renderBucket = renderBucket;
            _resourceSetCache = resourceSetCache;
            _whiteTexture = whiteTexture;

            ResourceFactory factory = gd.ResourceFactory;
            _vertexBuffer = new VertexList<QuadVertex>(gd, initialCapacity: 512 * 4);
            ResizeIndexBufferIfNecessary();

            _spriteResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));
            _spriteResourceSetDesc = new ResourceSetDescription(
                _spriteResourceLayout,
                new BindableResource[2]
                {
                    whiteTexture,
                    gd.LinearSampler
                }
            );
            ResourceSet solidColorResourceSet = factory.CreateResourceSet(_spriteResourceSetDesc);
            SolidColorMaterial = new QuadMaterial(solidColorResourceSet, Vector2.Zero, Vector2.One);

            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("quad");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );

            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSetDesc,
                new[] { viewProjection.ResourceLayout, _spriteResourceLayout },
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

            (Shader vs, Shader fs) grayscale = shaderLibrary.GetShaderSet("grayscale");
            var grayscaleSet = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { grayscale.vs, grayscale.fs }
            );
            pipelineDesc.BlendState = BlendStateDescription.SingleAlphaBlend;
            pipelineDesc.ShaderSet = grayscaleSet;
            _grayscalePipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
        }

        public QuadMaterial SolidColorMaterial { get; }

        private void ResizeIndexBufferIfNecessary()
        {
            Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 2, 1, 3 };
            uint indicesNeeded = 6 * (_vertexBuffer.Capacity / 4u);
            uint requiredSizeInBytes = indicesNeeded * sizeof(ushort);
            if (_indexBuffer == null || _indexBuffer.SizeInBytes != requiredSizeInBytes)
            {
                _indexBuffer?.Dispose();
                _indexBuffer = _resourceFactory.CreateBuffer(
                    new BufferDescription(requiredSizeInBytes, BufferUsage.IndexBuffer)
                );

                var indices = new ushort[indicesNeeded];
                for (int i = 0; i < indicesNeeded; i++)
                {
                    int quad = i / 6;
                    int vertexInQuad = i % 6;
                    indices[i] = (ushort)(quadIndices[vertexInQuad] + 4 * quad);
                }
                _gd.UpdateBuffer(_indexBuffer, 0, indices);
            }
        }

        public void BeginFrame()
        {
            _vertexBuffer.Begin();
        }

        public void EndFrame(CommandList cl)
        {
            _vertexBuffer.End(cl);
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
            ResourceSet commonResourceSet = _viewProjection.ResourceSet;
            int count = ribs.Length;
            uint quadCount = _vertexBuffer.Count / 4u; 
            for (int i = 0; i < count; i++)
            {
                Span<QuadVertex> vertices = _vertexBuffer.Append(4);
                // Covers the case when there's only 1 material used for all quads
                int materialIndex = materials.Length > 1 ? i : 0;
                QuadMaterial material = materials[materialIndex];
                Matrix4x4 transform = transforms[i];
                SizeF dstSize = dstSizes[i];
                ref readonly CommonItemProperties commonProps = ref ribs[i];
                var color = commonProps.Color.ToVector4();

                ref var VertexTL = ref vertices[0];
                VertexTL.Position.X = 0.0f;
                VertexTL.Position.Y = 0.0f;
                VertexTL.TexCoord.X = material.UvTopLeft.X;
                VertexTL.TexCoord.Y = material.UvTopLeft.Y;
                VertexTL.Position = Vector2.Transform(VertexTL.Position, transform);
                VertexTL.Color = color;

                ref var VertexTR = ref vertices[1];
                VertexTR.Position.X = dstSize.Width;
                VertexTR.Position.Y = 0.0f;
                VertexTR.TexCoord.X = material.UvBottomRight.X;
                VertexTR.TexCoord.Y = material.UvTopLeft.Y;
                VertexTR.Position = Vector2.Transform(VertexTR.Position, transform);
                VertexTR.Color = color;

                ref var VertexBL = ref vertices[2];
                VertexBL.Position.X = 0.0f;
                VertexBL.Position.Y = 0.0f + dstSize.Height;
                VertexBL.TexCoord.X = material.UvTopLeft.X;
                VertexBL.TexCoord.Y = material.UvBottomRight.Y;
                VertexBL.Position = Vector2.Transform(VertexBL.Position, transform);
                VertexBL.Color = color;

                ref var VertexBR = ref vertices[3];
                VertexBR.Position.X = dstSize.Width;
                VertexBR.Position.Y = dstSize.Height;
                VertexBR.TexCoord.X = material.UvBottomRight.X;
                VertexBR.TexCoord.Y = material.UvBottomRight.Y;
                VertexBR.Position = Vector2.Transform(VertexBR.Position, transform);
                VertexBR.Color = color;
            }

            var multiSub = _renderBucket.PrepareMultiSubmission((uint)count);
            for (int i = 0; i < count; i++)
            {
                CommonItemProperties commonProps = ribs[i];
                int materialIndex = materials.Length > 1 ? i : 0;
                Pipeline pipeline = (commonProps.BlendMode, commonProps.Effect) switch
                {
                    (BlendMode.Alpha, EffectKind.None) => _alphaBlendPipeline,
                    (BlendMode.Additive, EffectKind.None) => _additiveBlendPipeline,
                    (BlendMode.Subtractive, EffectKind.None) => _subtractiveBlendPipeline,
                    (BlendMode.Multiplicative, EffectKind.None) => _mulpiplicativeBlendPipeline,
                    (_, EffectKind.Grayscale) => _grayscalePipeline,
                    _ => ThrowHelper.UnexpectedValue<Pipeline>()
                };

                multiSub.Keys[i] = commonProps.Key;
                multiSub.Submissions[i] = new RenderBucketSubmission
                {
                    VertexBuffer0 = _vertexBuffer,
                    Pipeline = pipeline,
                    SharedResourceSet = commonResourceSet,
                    ObjectResourceSet0 = materials[materialIndex].ResourceSet,
                    InstanceCount = 1,
                    IndexBuffer = _indexBuffer,
                    IndexBase = (ushort)(6 * (quadCount + i)),
                    IndexCount = 6,
                    VertexCount = 6
                };
            }

            _renderBucket.Submit(multiSub);
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
