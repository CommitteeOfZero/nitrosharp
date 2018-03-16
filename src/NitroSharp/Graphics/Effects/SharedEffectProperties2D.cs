using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class SharedEffectProperties2D : BoundResourceSet
    {
        private Matrix4x4 _projection;

        public SharedEffectProperties2D(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
        public Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
        }
    }
}
