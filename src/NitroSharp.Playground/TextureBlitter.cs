using System;
using System.Numerics;
using NitroSharp.Graphics;
using Veldrid;

namespace NitroSharp.Playground
{
    internal sealed class TextureBlitter : IDisposable
    {
        private readonly GraphicsDevice _gd;
        private readonly ResourceLayout _rl;
        private readonly Pipeline _pipeline;

        public ResourceLayout ResourceLayout => _rl;

        public TextureBlitter(
            GraphicsDevice gd,
            ShaderLibrary shaderLib,
            string shaderSet,
            ResourceFactory factory,
            OutputDescription outputDesc)
        {
            _gd = gd;
            (Shader vs, Shader fs) = shaderLib.GetShaderSet(shaderSet);

            _rl = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    Array.Empty<VertexLayoutDescription>(),
                    new[] { vs, fs }
                ),
                new[] { _rl },
                outputDesc));
        }

        public void Render(CommandList cb, ResourceSet rs)
        {
            cb.SetPipeline(_pipeline);
            cb.SetGraphicsResourceSet(0, rs);
            cb.Draw(3);
        }

        public void Dispose()
        {
            _rl.Dispose();
            _pipeline.Dispose();
        }
    }
}
