using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class SpriteEffect : Effect2D
    {
        private TextureView _texture;
        private Sampler _sampler;
        private Matrix4x4 _projection, _transform;

        public SpriteEffect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 0)]
        protected override Matrix4x4 Transform
        {
            get => _transform;
            set => Update(ref _transform, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 0)]
        protected override Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
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
    }
}
