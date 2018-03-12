using System.Numerics;
using NitroSharp.Graphics.Framework;
using Veldrid;

namespace NitroSharp.Graphics.Effects
{
    public sealed class CubeEffect : Effect
    {
        private Matrix4x4 _projection, _view, _world;
        private TextureView _texture;
        private Sampler _sampler;

        public CubeEffect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 0)]
        public Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 0)]
        public Matrix4x4 View
        {
            get => _view;
            set => Update(ref _view, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 1)]
        public Matrix4x4 World
        {
            get => _world;
            set => Update(ref _world, value);
        }

        [BoundResource(ResourceKind.TextureReadOnly, ShaderStages.Fragment, resourceSet: 1)]
        public TextureView Texture
        {
            get => _texture;
            set => Set(ref _texture, value);
        }

        [BoundResource(ResourceKind.Sampler, ShaderStages.Fragment, resourceSet: 1)]
        public Sampler Sampler
        {
            get => _sampler;
            set => Set(ref _sampler, value);
        }

        protected override GraphicsPipelineDescription SetupPipeline()
        {
            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    Vertex3D.LayoutDescription
                },
                new Shader[]
                {
                    _vs,
                    _fs
                });

            return new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                _resourceLayouts,
                _gd.SwapchainFramebuffer.OutputDescription);
        }
    }
}
