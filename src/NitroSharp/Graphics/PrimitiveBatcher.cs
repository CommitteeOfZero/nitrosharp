using System;
using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal enum BlendMode
    {
        Alpha,
        Additive
    }

    internal sealed class PrimitiveBatcher : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly QuadGeometryStream _quadGeometryStream;
        private readonly SharedResources _sharedConstants;
        private readonly ResourceSetCache _resourceSetCache;
        private readonly Framebuffer _frameBuffer;

        private readonly RenderBucket _renderBucket;
        private Matrix4x4 _transform;

        private readonly ResourceLayout _spriteResourceLayout;
        private ResourceSetDescription _resourceSetDescription;
        private readonly Pipeline _alphaBlendPipeline;
        private readonly Pipeline _additiveBlendPipeline;

        private readonly Texture _whiteTexture;
        private readonly TextureView _whiteTextureView;

        private ResourceSet _lastResourceSet;
        private TextureView _lastTexture;

        public PrimitiveBatcher(
            GraphicsDevice graphicsDevice,
            RenderBucket renderBucket,
            QuadGeometryStream quadGeometryStream,
            ShaderLibrary shaderLibrary,
            SharedResources sharedConstants,
            ResourceSetCache resourceSetCache,
            Framebuffer framebuffer,
            uint initialCapacity = 512)
        {
            _frameBuffer = framebuffer;
            _gd = graphicsDevice;
            _quadGeometryStream = quadGeometryStream;
            _sharedConstants = sharedConstants;
            _resourceSetCache = resourceSetCache;

            _renderBucket = renderBucket;

            ResourceFactory factory = _gd.ResourceFactory;
            _spriteResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _resourceSetDescription = new ResourceSetDescription(_spriteResourceLayout, new BindableResource[2]);
            _resourceSetDescription.BoundResources[1] = _gd.LinearSampler;

            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("TexturedQuad");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription, QuadInstanceData.LayoutDescription },
                new[] { vs, fs });

            var pipelineDesc = new GraphicsPipelineDescription(
                    BlendStateDescription.SingleAlphaBlend,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleList,
                    shaderSetDesc,
                    new[] { sharedConstants.Layout, _spriteResourceLayout },
                    _frameBuffer.OutputDescription);

            _alphaBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
            pipelineDesc.BlendState = BlendStateDescription.SingleAdditiveBlend;
            _additiveBlendPipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

            var stagingWhite = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                 width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            var pixels = _gd.Map<RgbaByte>(stagingWhite, MapMode.Write);
            pixels[0] = RgbaByte.White;
            _gd.Unmap(stagingWhite);

            _whiteTexture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                 width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            _whiteTextureView = _gd.ResourceFactory.CreateTextureView(_whiteTexture);

            CommandList cl = _gd.ResourceFactory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(stagingWhite, _whiteTexture);
            cl.End();
            _gd.SubmitCommands(cl);
            _gd.DisposeWhenIdle(cl);
            _gd.DisposeWhenIdle(stagingWhite);
        }

        public void SetTransform(in Matrix4x4 transform)
        {
            _transform = transform;
        }

        public void FillRectangle(float x, float y, float width, float height, ref RgbaFloat fillColor)
            => FillRectangle(new RectangleF(x, y, width, height), ref fillColor);

        public void FillRectangle(in RectangleF rect, ref RgbaFloat fillColor)
            => DrawImage(_whiteTextureView, null, rect, ref fillColor);

        public void DrawImage(
            TextureView textureView,
            in RectangleF? sourceRect,
            in RectangleF destinationRect,
            ref RgbaFloat color,
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
                _resourceSetDescription.BoundResources[0] = textureView;
                resourceSet = _resourceSetCache.GetResourceSet(ref _resourceSetDescription);
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
                SharedResourceSet = _sharedConstants.OrthographicProjection,
                ObjectResourceSet = resourceSet
            };

            _renderBucket.Submit(ref submission, 0);
        }

        public void Dispose()
        {
            _alphaBlendPipeline.Dispose();
            _additiveBlendPipeline.Dispose();
            _spriteResourceLayout.Dispose();
        }

        //public void DrawImage(TextureView image, float x, float y, int depth, float opacity = 1.0f)
        //    => DrawImage(image, new RectangleF(x, y, image.Target.Width, image.Target.Height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity), depth);

        //public void DrawImage(TextureView image, float x, float y, in RgbaFloat color, int depth)
        //    => DrawImage(image, new RectangleF(x, y, image.Target.Width, image.Target.Height), color, depth);

        //public void DrawImage(TextureView image, float x, float y, float width, float height, int depth, float opacity = 1.0f)
        //    => DrawImage(image, null, new RectangleF(x, y, width, height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity), depth);

        //public void DrawImage(TextureView image, in RectangleF rect, int depth, float opacity = 1.0f)
        //    => DrawImage(image, rect, new RgbaFloat(1.0f, 1.0f, 1.0f, opacity), depth);

        //public void DrawImage(TextureView image, in RectangleF rect, in RgbaFloat color, int depth)
        //    => DrawImage(image, null, rect, color, depth);

    }
}
