using System.Numerics;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;

#pragma warning disable 649

[assembly: ShaderSet("Fill", "NitroSharp.Graphics.Shaders.Fill.VS", "NitroSharp.Graphics.Shaders.Fill.FS")]

namespace NitroSharp.Graphics.Shaders
{
    internal class Fill
    {
        [ResourceSet(0)]
        public Matrix4x4 Projection;

        [ResourceSet(1)]
        public Matrix4x4 Transform;

        [VertexShader]
        public FragmentInput VS(VertexInput2D input)
        {
            FragmentInput output;
            output.SystemPosition = Mul(Transform, new Vector4(input.Position, 0, 1));
            output.SystemPosition = Mul(Projection, output.SystemPosition);
            output.TexCoords = input.TexCoords;
            output.Color = input.Color;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return input.Color;
        }
    }
}
