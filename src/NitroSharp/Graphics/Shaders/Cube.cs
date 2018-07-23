using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

#pragma warning disable 649

[assembly: ShaderSet("Cube", "NitroSharp.Graphics.Shaders.Cube.VS", "NitroSharp.Graphics.Shaders.Cube.FS")]

namespace NitroSharp.Graphics.Shaders
{
    internal class Cube
    {
        [ResourceSet(0)]
        public Matrix4x4 ViewProjection;

        [ResourceSet(1)]
        public TextureCubeResource Texture;
        [ResourceSet(1)]
        public SamplerResource Sampler;

        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            Vector4 col1 = input.Col1;
            Vector4 col2 = input.Col2;
            Vector4 col3 = input.Col3;
            Vector4 col4 = input.Col4;
            var world = new Matrix4x4(
                col1.X, col2.X, col3.X, col4.X,
                col1.Y, col2.Y, col3.Y, col4.Y,
                col1.Z, col2.Z, col3.Z, col4.Z,
                col1.W, col2.W, col3.W, col4.W);

            //var world = new Matrix4x4(
            //   col1.X, col1.Y, col1.Z, col1.W,
            //   col2.X, col2.Y, col2.Z, col2.W,
            //   col3.X, col3.Y, col3.Z, col3.W,
            //   col4.X, col4.Y, col4.Z, col4.W);

            output.SystemPosition = Mul(ViewProjection, Mul(world, new Vector4(input.Position, 1)));
            Vector3 pos = input.Position;
            output.TexCoords = new Vector3(-pos.X, pos.Y, pos.Z);
            output.Color = input.Color;

            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return Sample(Texture, Sampler, input.TexCoords) * input.Color;
        }

        public struct VertexInput
        {
            [TextureCoordinateSemantic] public Vector3 Position;

            // Per-instance
            [TextureCoordinateSemantic] public Vector4 Color;
            [TextureCoordinateSemantic] public Vector4 Col1;
            [TextureCoordinateSemantic] public Vector4 Col2;
            [TextureCoordinateSemantic] public Vector4 Col3;
            [TextureCoordinateSemantic] public Vector4 Col4;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic] public Vector4 SystemPosition;
            [TextureCoordinateSemantic] public Vector3 TexCoords;
            [ColorSemantic] public Vector4 Color;
        }
    }
}
