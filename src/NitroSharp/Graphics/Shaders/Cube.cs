using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

#pragma warning disable 649

[assembly: ShaderSet("Cube", "NitroSharp.Graphics.Shaders.Cube.VS", "NitroSharp.Graphics.Shaders.Cube.FS")]

namespace NitroSharp.Graphics.Shaders
{
    public class Cube
    {
        [ResourceSet(0)]
        public Matrix4x4 View;
        [ResourceSet(0)]
        public Matrix4x4 Projection;

        [ResourceSet(1)]
        public Matrix4x4 World;
        [ResourceSet(1)]
        public TextureCubeResource Texture;
        [ResourceSet(1)]
        public SamplerResource Sampler;

        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            //Matrix4x4 view = new Matrix4x4(
            //    1, 0, 0, 0,
            //    0, 1, 0, 0,
            //    0, 0, 1, 0,
            //    0, 0, 1, 1);

            FragmentInput output;
            Vector4 worldPosition = Mul(World, new Vector4(input.Position, 1));
            Vector4 viewPosition = Mul(View, worldPosition);
            Vector4 clipPosition = Mul(Projection, viewPosition);
            output.SystemPosition = clipPosition;

            var pos = input.Position;
            output.TexCoords = new Vector3(-pos.X, pos.Y, pos.Z);

            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return Sample(Texture, Sampler, input.TexCoords);
        }

        public struct VertexInput
        {
            [PositionSemantic] public Vector3 Position;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic] public Vector4 SystemPosition;
            [TextureCoordinateSemantic] public Vector3 TexCoords;
        }
    }
}
