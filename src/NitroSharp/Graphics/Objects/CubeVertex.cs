using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct CubeVertex
    {
        public readonly SimpleVector3 Position;

        public CubeVertex(in SimpleVector3 position)
        {
            Position = position;
        }

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3));
    }
}
