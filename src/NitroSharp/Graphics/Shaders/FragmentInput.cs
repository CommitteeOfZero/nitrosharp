using ShaderGen;
using System.Numerics;

namespace NitroSharp.Graphics.Shaders
{
    internal struct FragmentInput
    {
        [SystemPositionSemantic] public Vector4 SystemPosition;
        [TextureCoordinateSemantic] public Vector2 TexCoords;
        [ColorSemantic] public Vector4 Color;
    }
}
