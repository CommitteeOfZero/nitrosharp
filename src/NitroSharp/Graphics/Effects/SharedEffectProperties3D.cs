using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal class SharedEffectProperties3D : BoundResourceSet
    {
        private Matrix4x4 _view, _projection;

        public SharedEffectProperties3D(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
        public Matrix4x4 View
        {
            get => _view;
            set => Update(ref _view, value);
        }

        [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
        public Matrix4x4 Projection
        {
            get => _projection;
            set => Update(ref _projection, value);
        }
    }
}
