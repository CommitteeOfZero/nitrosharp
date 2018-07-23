using System.Numerics;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;

#pragma warning disable 649

[assembly: ShaderSet("TexturedQuad", "NitroSharp.Graphics.Shaders.TexturedQuad.VS", "NitroSharp.Graphics.Shaders.TexturedQuad.FS")]

namespace NitroSharp.Graphics.Shaders
{
    internal class TexturedQuad
    {
        [ResourceSet(0)]
        public Matrix4x4 ViewProjection;

        [ResourceSet(1)]
        public Texture2DResource Texture;
        [ResourceSet(1)]
        public SamplerResource Sampler;

        [VertexShader]
        public FSInput VS(VSInput input)
        {
            FSInput output;
            output.SystemPosition = Mul(ViewProjection, new Vector4(input.Position, 0, 1));
            output.TexCoord = input.TexCoord;
            output.Color = input.Color;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FSInput input)
        {
            return Sample(Texture, Sampler, input.TexCoord) * input.Color;
        }

        internal struct VSInput
        {
            [PositionSemantic] public Vector2 Position;
            [TextureCoordinateSemantic] public Vector2 TexCoord;

            // Per-instance data
            [ColorSemantic] public Vector4 Color;
        }

        internal struct FSInput
        {
            [SystemPositionSemantic] public Vector4 SystemPosition;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
            [ColorSemantic] public Vector4 Color;
        }
    }
}
