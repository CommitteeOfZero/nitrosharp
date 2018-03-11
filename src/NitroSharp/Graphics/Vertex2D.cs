using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct Vertex2D
    {
        public const uint SizeInBytes = 32;

        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaFloat Color;

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
    }
}
