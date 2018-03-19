using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class SpriteEffect : Effect
    {
        public SpriteEffect(
            GraphicsDevice graphicsDevice,
            Shader vertexShader, Shader fragmentShader,
            SharedEffectProperties2D sharedProperties)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
            Properties = new EffectProperties(graphicsDevice);
            Initialize(sharedProperties, Properties);
        }

        public EffectProperties Properties { get; }
        protected override VertexLayoutDescription VertexLayout => Vertex2D.LayoutDescription;

        public sealed class EffectProperties : BoundResourceSet
        {
            private Matrix4x4 _transform;
            private TextureView _texture;
            private Sampler _sampler;

            public EffectProperties(GraphicsDevice graphicsDevice) : base(graphicsDevice)
            {
            }

            [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
            public Matrix4x4 Transform
            {
                get => _transform;
                set => Update(ref _transform, value);
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
    }
}
