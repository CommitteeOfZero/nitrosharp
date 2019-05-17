using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Primitives;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal enum BlendMode
    {
        Alpha,
        Additive
    }

    internal sealed class QuadBatcher : IDisposable
    {
        private readonly RenderBucket<RenderItemKey> _renderBucket;
        private readonly QuadGeometryStream _quadGeometryStream;
        private readonly ViewProjection _viewProjection;
        private readonly ResourceSetCache _resourceSetCache;
        private readonly TextureView _whiteTextureView;

        private readonly ResourceLayout _spriteResourceLayout;
        private ResourceSetDescription _spriteResourceSetDesc;
        private readonly Pipeline _alphaBlendPipeline;
        private readonly Pipeline _additiveBlendPipeline;

        private ResourceSet _lastResourceSet;
        private TextureView _lastTexture;
        private Matrix4x4 _transform;

        public QuadBatcher(
            GraphicsDevice graphicsDevice,
            Framebuffer framebuffer,
            ViewProjection viewProjection,
            RenderBucket<RenderItemKey> renderBucket,
            QuadGeometryStream quadGeometryStream,
            ResourceSetCache resourceSetCache,
            ShaderLibrary shaderLibrary,
            TextureView whiteTexture)
        {
            GraphicsDevice gd = graphicsDevice;
            _viewProjection = viewProjection;
            _renderBucket = renderBucket;
            _quadGeometryStream = quadGeometryStream;
            _resourceSetCache = resourceSetCache;
            _whiteTextureView = whiteTexture;

            ResourceFactory factory = gd.ResourceFactory;
            _spriteResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _spriteResourceSetDesc = new ResourceSetDescription(_spriteResourceLayout, new BindableResource[2]);
            _spriteResourceSetDesc.BoundResources[1] = gd.LinearSampler;

            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("quad");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription, QuadInstanceData.LayoutDescription },
                new[] { vs, fs });

            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSetDesc,
                new[] { viewProjection.ResourceLayout, _spriteResourceLayout },
                framebuffer.OutputDescription);

            _alphaBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
            pipelineDesc.BlendState = BlendStateDescription.SingleAdditiveBlend;
            _additiveBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTransform(in Matrix4x4 transform)
        {
            _transform = transform;
        }

        public void FillRectangle(float x, float y, float width, float height, ref RgbaFloat fillColor, RenderItemKey renderPriority)
            => FillRectangle(new RectangleF(x, y, width, height), ref fillColor, renderPriority);

        public void FillRectangle(in RectangleF rect, ref RgbaFloat fillColor, RenderItemKey renderPriority)
            => DrawImage(_whiteTextureView, null, rect, ref fillColor, renderPriority);

        public void DrawImage(
            TextureView textureView,
            in RectangleF? sourceRect,
            in RectangleF destinationRect,
            ref RgbaFloat color,
            RenderItemKey renderPriority,
            BlendMode blendMode = BlendMode.Alpha)
        {
            var sourceRectangle = sourceRect ?? new RectangleF(
                0, 0, (int)textureView.Target.Width, (int)textureView.Target.Height);

            var uvTopLeft = new Vector2(
                    sourceRectangle.Left / textureView.Target.Width,
                    sourceRectangle.Top / textureView.Target.Height);

            var uvBottomRight = new Vector2(
                sourceRectangle.Right / textureView.Target.Width,
                sourceRectangle.Bottom / textureView.Target.Height);

            (ushort vertexBase, ushort instanceBase) = _quadGeometryStream.Append(
                destinationRect, uvTopLeft, uvBottomRight, _transform, ref color);

            ResourceSet resourceSet = _lastResourceSet;
            if (textureView != _lastTexture)
            {
                _spriteResourceSetDesc.BoundResources[0] = textureView;
                resourceSet = _resourceSetCache.GetResourceSet(ref _spriteResourceSetDesc);
                _lastTexture = textureView;
                _lastResourceSet = resourceSet;
            }

            var submission = new RenderBucketSubmission<QuadVertex, QuadInstanceData>
            {
                VertexBuffer = _quadGeometryStream.VertexBuffer,
                IndexBuffer = _quadGeometryStream.IndexBuffer,
                VertexBase = vertexBase,
                VertexCount = 4,
                IndexBase = 0,
                IndexCount = 6,
                InstanceDataBuffer = _quadGeometryStream.InstanceDataBuffer,
                InstanceBase = instanceBase,
                Pipeline = blendMode == BlendMode.Alpha ? _alphaBlendPipeline : _additiveBlendPipeline,
                SharedResourceSet = _viewProjection.ResourceSet,
                ObjectResourceSet = resourceSet
            };

            _renderBucket.Submit(ref submission, renderPriority);
        }

        public void Dispose()
        {
            _alphaBlendPipeline.Dispose();
            _additiveBlendPipeline.Dispose();
            _spriteResourceLayout.Dispose();
        }
    }
}
