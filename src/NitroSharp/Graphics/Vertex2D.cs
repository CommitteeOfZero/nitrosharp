using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct Vertex2D
    {
        public const uint SizeInBytes = 32;

        public SimpleVector2 Position;
        public SimpleVector2 TexCoords;
        public RgbaFloat Color;

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
            new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
    }
}
