using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal class FillEffect : Effect2D
    {
        private Matrix4x4 _transform, _projection;

        public FillEffect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
        public override Matrix4x4 Transform
        {
            get => _transform;
            set => Update(ref _transform, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
        public override Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
        }
    }
}
