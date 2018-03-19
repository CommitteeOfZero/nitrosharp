using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct CubeVertex
    {
        public readonly Vector3 Position;

        public CubeVertex(in Vector3 position)
        {
            Position = position;
        }

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3));
    }
}
