using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal class FillEffect : Effect
    {
        public FillEffect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader, SharedEffectProperties2D sharedProperties)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
            Properties = new EffectProperties(graphicsDevice);
            Initialize(sharedProperties, Properties);
        }

        public EffectProperties Properties { get; }

        public class EffectProperties : BoundResourceSet
        {
            private Matrix4x4 _transform;

            public EffectProperties(GraphicsDevice graphicsDevice) : base(graphicsDevice)
            {
            }

            [BoundResource(ResourceKind.UniformBuffer, ShaderStages.Vertex)]
            public Matrix4x4 Transform
            {
                get => _transform;
                set => Update(ref _transform, value);
            }
        }
    }
}
