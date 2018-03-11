using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class FadeMaskEffect : Effect
    {
        private Matrix4x4 _projection;
        private TextureView _source, _mask;
        private Sampler _sampler;
        private FadeMaskProperties _properties;

        public FadeMaskEffect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex, resourceSet: 0)]
        public Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
        }

        [BoundResource(ResourceKind.TextureReadOnly, ShaderStages.Fragment, resourceSet: 1)]
        public TextureView Source
        {
            get => _source;
            set => Set(ref _source, value);
        }

        [BoundResource(ResourceKind.TextureReadOnly, ShaderStages.Fragment, resourceSet: 1)]
        public TextureView Mask
        {
            get => _mask;
            set => Set(ref _mask, value);
        }

        [BoundResource(ResourceKind.Sampler, ShaderStages.Fragment, resourceSet: 1)]
        public Sampler Sampler
        {
            get => _sampler;
            set => Set(ref _sampler, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Fragment, resourceSet: 1)]
        public FadeMaskProperties Properties
        {
            get => _properties;
            set => Update(ref _properties, value);
        }
    }
}
