using System;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal readonly struct CubeVertex
    {
        public readonly Vector3 Position;

        public CubeVertex(float x, float y, float z)
            => Position = new Vector3(x, y, z);

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
            ),
            new VertexElementDescription(
                "vs_Color",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4
            )
        );
    }

    internal sealed class Cube : RenderItem3D
    {
        public static ReadOnlySpan<CubeVertex> Vertices => new[]
        {
            // Top
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(-0.5f,0.5f,0.5f),
            // Bottom
            new CubeVertex(-0.5f,-0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            // Left
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            // Right
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            // Back
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            // Front
            new CubeVertex(-0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,0.5f),
        };

        public static ushort[] Indices => new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        };

        public Cube(in ResolvedEntityPath path, int priority)
            : base(in path, priority)
        {
        }
    }
}
