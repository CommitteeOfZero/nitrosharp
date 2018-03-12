using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics.Framework
{
    public struct Vertex3D
    {
        public const byte SizeInBytes = 12;
        public const byte ElementCount = 1;

        public readonly Vector3 Position;

        public Vertex3D(Vector3 position)
        {
            Position = position;
        }

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3));
    }
}
