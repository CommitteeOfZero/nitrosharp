using ShaderGen;
using System.Numerics;

#pragma warning disable 649

namespace NitroSharp.Graphics.Shaders
{
    internal struct VertexInput2D
    {
        [PositionSemantic] public Vector2 Position;
        [TextureCoordinateSemantic] public Vector2 TexCoords;
        [ColorSemantic] public Vector4 Color;
    }
}
