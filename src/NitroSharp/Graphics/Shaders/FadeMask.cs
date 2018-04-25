using ShaderGen;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

#pragma warning disable 649

[assembly: ShaderSet("FadeMask", "NitroSharp.Graphics.Shaders.FadeMask.VS", "NitroSharp.Graphics.Shaders.FadeMask.FS")]

namespace NitroSharp.Graphics.Shaders
{
    internal class FadeMask
    {
        [ResourceSet(0)]
        public Matrix4x4 Projection;

        [ResourceSet(1)]
        public Texture2DResource Source;
        [ResourceSet(1)]
        public Texture2DResource Mask;
        [ResourceSet(1)]
        public SamplerResource Sampler;
        [ResourceSet(1)]
        public FadeMaskProperties Properties;

        [VertexShader]
        public FragmentInput VS(VertexInput2D input)
        {
            FragmentInput output;
            output.SystemPosition = Mul(Projection, new Vector4(input.Position, 0, 1));
            output.TexCoords = input.TexCoords;
            output.Color = input.Color;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            var srcPixel = Sample(Source, Sampler, input.TexCoords);
            var maskPixel = Sample(Mask, Sampler, input.TexCoords);
            float mask = maskPixel.X;

            float alpha;
            if (Properties.FadeAmount >= mask)
            {
                alpha = 1.0f;
            }
            else if (Properties.FadeAmount + Properties.Feather >= mask)
            {
                alpha = (Properties.Feather + Properties.FadeAmount - mask) / Properties.Feather;
            }
            else
            {
                alpha = 0.0f;
            }

            return srcPixel * alpha;
        }
    }
}
