using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics.Effects
{
    public sealed class CubeEffect : Effect
    {
        public CubeEffect(
            GraphicsDevice graphicsDevice,
            Shader vertexShader, Shader fragmentShader,
            SharedEffectProperties3D sharedProperties)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
            Properties = new EffectProperties(graphicsDevice);
            Initialize(sharedProperties, Properties);
        }

        public EffectProperties Properties { get; }

        public sealed class EffectProperties : BoundResourceSet
        {
            private Matrix4x4 _world;
            private TextureView _texture;
            private Sampler _sampler;

            public EffectProperties(GraphicsDevice graphicsDevice) : base(graphicsDevice)
            {
            }

            [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
            public Matrix4x4 World
            {
                get => _world;
                set => Update(ref _world, value);
            }

            [BoundResource(ResourceKind.TextureReadOnly, ShaderStages.Fragment)]
            public TextureView Texture
            {
                get => _texture;
                set => Set(ref _texture, value);
            }

            [BoundResource(ResourceKind.Sampler, ShaderStages.Fragment)]
            public Sampler Sampler
            {
                get => _sampler;
                set => Set(ref _sampler, value);
            }
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
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSet,
                _resourceLayouts,
                _gd.SwapchainFramebuffer.OutputDescription);
        }
    }
}
