using System;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class Pipelines : IDisposable
    {
        public Pipelines(
            ResourceFactory factory,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ViewProjection vp)
        {
            CommonResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "AlphaMask",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            SimpleEffectInputLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Input",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            TransitionInputLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
               new ResourceLayoutElementDescription(
                   "Input",
                   ResourceKind.TextureReadOnly,
                   ShaderStages.Fragment
               ),
               new ResourceLayoutElementDescription(
                   "Mask",
                   ResourceKind.TextureReadOnly,
                   ShaderStages.Fragment
               ),
               new ResourceLayoutElementDescription(
                   "Sampler",
                   ResourceKind.Sampler,
                   ShaderStages.Fragment
               )
            ));

            TransitionParamLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "FadeAmount",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Fragment
                )
            ));

            BarrelDistortionInputLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "LensTexture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("quad");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );

            var premultipliedAlpha = new BlendStateDescription
            {
                AttachmentStates = new[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.One,
                        DestinationColorFactor = BlendFactor.InverseSourceAlpha,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = BlendFactor.SourceAlpha,
                        DestinationAlphaFactor = BlendFactor.DestinationAlpha,
                        AlphaFunction = BlendFunction.Add
                    }
                }
            };
            var pipelineDesc = new GraphicsPipelineDescription(
                premultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSetDesc,
                new[] { vp.ResourceLayout, CommonResourceLayout },
                outputDescription
            );
            AlphaBlend = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.BlendState = new BlendStateDescription
            {
                AttachmentStates = new[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.One,
                        DestinationColorFactor = BlendFactor.One,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = BlendFactor.SourceAlpha,
                        DestinationAlphaFactor = BlendFactor.DestinationAlpha,
                        AlphaFunction = BlendFunction.Add
                    }
                }
            };
            AdditiveBlend = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.BlendState = new BlendStateDescription
            {
                AttachmentStates = new[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.One,
                        DestinationColorFactor = BlendFactor.One,
                        ColorFunction = BlendFunction.ReverseSubtract,
                        SourceAlphaFactor = BlendFactor.One,
                        DestinationAlphaFactor = BlendFactor.One,
                        AlphaFunction = BlendFunction.Subtract
                    }
                }
            };
            ReverseSubtractiveBlend = factory.CreateGraphicsPipeline(ref pipelineDesc);

            pipelineDesc.BlendState = new BlendStateDescription
            {
                AttachmentStates = new[]
                {
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = BlendFactor.DestinationColor,
                        DestinationColorFactor = BlendFactor.InverseSourceAlpha,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = BlendFactor.SourceAlpha,
                        DestinationAlphaFactor = BlendFactor.DestinationAlpha,
                        AlphaFunction = BlendFunction.Add,
                    }
                }
            };
            MultiplicativeBlend = factory.CreateGraphicsPipeline(ref pipelineDesc);

            (vs, fs) = shaderLibrary.LoadShaderSet("transition");
            var transitionShaderSet = new ShaderSetDescription(
               new[] { QuadVertex.LayoutDescription },
               new[] { vs, fs }
            );
            pipelineDesc = new GraphicsPipelineDescription(
                premultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                transitionShaderSet,
                new[]
                {
                    vp.ResourceLayout,
                    TransitionInputLayout,
                    TransitionParamLayout
                },
                outputDescription
            );
            Transition = factory.CreateGraphicsPipeline(ref pipelineDesc);

            (vs, fs) = shaderLibrary.LoadShaderSet("lens");
            var lensShaderSet = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );
            var lensPipelineDesc = new GraphicsPipelineDescription(
                premultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                lensShaderSet,
                new[] { vp.ResourceLayout, BarrelDistortionInputLayout },
                outputDescription
            );
            BarrelDistortion = factory.CreateGraphicsPipeline(ref lensPipelineDesc);

            Pipeline createEffect(string shaderSet, ResourceLayout effectLayout)
            {
                (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet(shaderSet);
                pipelineDesc.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                pipelineDesc.ShaderSet = new ShaderSetDescription(
                    Array.Empty<VertexLayoutDescription>(),
                    new[] { vs, fs }
                );
                pipelineDesc.ResourceLayouts = new[] { effectLayout };
                return factory.CreateGraphicsPipeline(ref pipelineDesc);
            }

            Blit = createEffect("blit", SimpleEffectInputLayout);
            Grayscale = createEffect("grayscale", SimpleEffectInputLayout);
            BoxBlur = createEffect("boxblur", SimpleEffectInputLayout);
        }

        public ResourceLayout CommonResourceLayout { get; }
        public ResourceLayout TransitionParamLayout { get; }
        public ResourceLayout SimpleEffectInputLayout { get; }
        public ResourceLayout BarrelDistortionInputLayout { get; }
        public ResourceLayout TransitionInputLayout { get; }
        public Pipeline AlphaBlend { get; }
        public Pipeline AdditiveBlend { get; }
        public Pipeline ReverseSubtractiveBlend { get; }
        public Pipeline MultiplicativeBlend { get; }
        public Pipeline Transition { get; }
        public Pipeline Blit { get; }
        public Pipeline Grayscale { get; }
        public Pipeline BoxBlur { get; }
        public Pipeline BarrelDistortion { get; }

        public void Dispose()
        {
            BarrelDistortion.Dispose();
            AlphaBlend.Dispose();
            AdditiveBlend.Dispose();
            ReverseSubtractiveBlend.Dispose();
            MultiplicativeBlend.Dispose();
            Transition.Dispose();
            Blit.Dispose();
            Grayscale.Dispose();
            BoxBlur.Dispose();
            CommonResourceLayout.Dispose();
            TransitionParamLayout.Dispose();
            SimpleEffectInputLayout.Dispose();
            TransitionInputLayout.Dispose();
            BarrelDistortionInputLayout.Dispose();
        }
    }
}
