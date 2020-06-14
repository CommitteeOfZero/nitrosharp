using System;
using System.Numerics;
using NitroSharp.Graphics.Core;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class ShaderResources : IDisposable
    {
        public static BlendStateDescription PremultipliedAlpha => new BlendStateDescription
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

        public ShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ResourceLayout viewProjectionLayout)
        {
            Quad = new QuadShaderResources(graphicsDevice, shaderLibrary, outputDescription, viewProjectionLayout);
            Transition = new TransitionShaderResources(graphicsDevice, shaderLibrary, outputDescription, viewProjectionLayout);
            Text = new TextShaderResources(graphicsDevice, shaderLibrary, outputDescription);
            Effects = new EffectShaderResources(graphicsDevice, shaderLibrary, outputDescription);
            BarrelDistortion = new BarrelDistortionShaderResources(graphicsDevice, shaderLibrary, outputDescription, viewProjectionLayout);
            Cube = new CubeShaderResources(graphicsDevice, shaderLibrary, outputDescription, viewProjectionLayout);
        }

        public QuadShaderResources Quad { get; }
        public TransitionShaderResources Transition { get; }
        public TextShaderResources Text { get; }
        public EffectShaderResources Effects { get; }
        public BarrelDistortionShaderResources BarrelDistortion { get; }
        public CubeShaderResources Cube { get; }

        public void Dispose()
        {
            Quad.Dispose();
            Transition.Dispose();
            Text.Dispose();
            Effects.Dispose();
            BarrelDistortion.Dispose();
            Cube.Dispose();
        }
    }

    internal sealed class QuadShaderResources : IDisposable
    {
        public QuadShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ResourceLayout viewProjectionLayout)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
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
                ),
                new ResourceLayoutElementDescription(
                    "AlphaMaskPos",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Fragment
                )
            ));

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("quad");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );

            var pipelineDesc = new GraphicsPipelineDescription(
                ShaderResources.PremultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                shaderSetDesc,
                new[] { viewProjectionLayout, ResourceLayout },
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

            AlphaMaskPositionBuffer = new GpuBuffer<Vector4>(
                graphicsDevice,
                BufferUsage.UniformBuffer | BufferUsage.Dynamic,
                Vector4.Zero
            );
        }

        public ResourceLayout ResourceLayout { get; }
        public Pipeline AlphaBlend { get; }
        public Pipeline AdditiveBlend { get; }
        public Pipeline ReverseSubtractiveBlend { get; }
        public Pipeline MultiplicativeBlend { get; }

        public GpuBuffer<Vector4> AlphaMaskPositionBuffer { get; }

        public Pipeline GetPipeline(BlendMode blendMode) => blendMode switch
        {
            BlendMode.Alpha => AlphaBlend,
            BlendMode.Additive => AdditiveBlend,
            BlendMode.ReverseSubtractive => ReverseSubtractiveBlend,
            BlendMode.Multiplicative => MultiplicativeBlend,
            _ => ThrowHelper.UnexpectedValue<Pipeline>()
        };

        public void Dispose()
        {
            AlphaBlend.Dispose();
            AdditiveBlend.Dispose();
            ReverseSubtractiveBlend.Dispose();
            MultiplicativeBlend.Dispose();
            ResourceLayout.Dispose();
            AlphaMaskPositionBuffer.Dispose();
        }
    }

    internal sealed class TransitionShaderResources : IDisposable
    {
        public TransitionShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ResourceLayout viewProjectionLayout)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            InputLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
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

            ParamLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "FadeAmount",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Fragment
                )
            ));

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("transition");
            var transitionShaderSet = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );
            var pipelineDesc = new GraphicsPipelineDescription(
                ShaderResources.PremultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                transitionShaderSet,
                new[]
                {
                    viewProjectionLayout,
                    InputLayout,
                    ParamLayout
                },
                outputDescription
            );
            Pipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
            ProgressBuffer = new GpuBuffer<Vector4>(
                graphicsDevice,
                BufferUsage.UniformBuffer | BufferUsage.Dynamic,
                data: Vector4.Zero
            );
        }

        public ResourceLayout InputLayout { get; }
        public ResourceLayout ParamLayout { get; }
        public Pipeline Pipeline { get; }

        public GpuBuffer<Vector4> ProgressBuffer { get; }

        public void Dispose()
        {
            Pipeline.Dispose();
            InputLayout.Dispose();
            ParamLayout.Dispose();
            ProgressBuffer.Dispose();
        }
    }

    internal sealed class TextShaderResources : IDisposable
    {
        public TextShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ResourceLayoutVS = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "GlyphRuns",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "Transforms",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                ),
                new ResourceLayoutElementDescription(
                    "GlyphRects",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Vertex
                )
            ));

            ResourceLayoutFS = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "CacheTexture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("text");
            (Shader outlineVS, Shader outlineFS) = shaderLibrary.LoadShaderSet("outline");
            var pipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[] { GpuGlyph.LayoutDescription },
                    new[] { vs, fs }
                ),
                new[] { ResourceLayoutVS, ResourceLayoutFS },
                outputDescription
            );
            Pipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
            pipelineDesc.ShaderSet.Shaders = new[] { outlineVS, outlineFS };
            OutlinePipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);
        }

        public ResourceLayout ResourceLayoutVS { get; }
        public ResourceLayout ResourceLayoutFS { get; }
        public Pipeline Pipeline { get; }
        public Pipeline OutlinePipeline { get; }

        public void Dispose()
        {
            Pipeline.Dispose();
            OutlinePipeline.Dispose();
            ResourceLayoutVS.Dispose();
            ResourceLayoutFS.Dispose();
        }
    }

    internal sealed class EffectShaderResources : IDisposable
    {
        public EffectShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            OutputDescription outputDescription)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
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

            Blit = createPipeline("blit", ResourceLayout);
            Grayscale = createPipeline("grayscale", ResourceLayout);
            BoxBlur = createPipeline("boxblur", ResourceLayout);

            Pipeline createPipeline(string shaderSetName, ResourceLayout layout)
            {
                (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet(shaderSetName);
                var shaderSetDesc = new ShaderSetDescription(
                    Array.Empty<VertexLayoutDescription>(),
                    new[] { vs, fs }
                );
                var pipelineDesc = new GraphicsPipelineDescription(
                    ShaderResources.PremultipliedAlpha,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleStrip,
                    shaderSetDesc,
                    new[] { layout },
                    outputDescription
                );
                return factory.CreateGraphicsPipeline(ref pipelineDesc);
            }
        }

        public ResourceLayout ResourceLayout { get; }
        public Pipeline Blit { get; }
        public Pipeline Grayscale { get; }
        public Pipeline BoxBlur { get; }

        public Pipeline GetPipeline(EffectKind effect)
        {
            return effect switch
            {
                EffectKind.Blit => Blit,
                EffectKind.Grayscale => Grayscale,
                EffectKind.BoxBlur => BoxBlur,
                _ => ThrowHelper.UnexpectedValue<Pipeline>()
            };
        }

        public void Dispose()
        {
            Blit.Dispose();
            Grayscale.Dispose();
            BoxBlur.Dispose();
            ResourceLayout.Dispose();
        }
    }

    internal sealed class BarrelDistortionShaderResources : IDisposable
    {
        public BarrelDistortionShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ResourceLayout viewProjectionLayout)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
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

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("lens");
            var lensShaderSet = new ShaderSetDescription(
                new[] { QuadVertex.LayoutDescription },
                new[] { vs, fs }
            );
            var lensPipelineDesc = new GraphicsPipelineDescription(
                ShaderResources.PremultipliedAlpha,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                lensShaderSet,
                new[] { viewProjectionLayout, ResourceLayout },
                outputDescription
            );
            Pipeline = factory.CreateGraphicsPipeline(ref lensPipelineDesc);
        }

        public ResourceLayout ResourceLayout { get; }
        public Pipeline Pipeline { get; }

        public void Dispose()
        {
            Pipeline.Dispose();
            ResourceLayout.Dispose();
        }
    }

    internal sealed class CubeShaderResources : IDisposable
    {
        public CubeShaderResources(
            GraphicsDevice graphicsDevice,
            ShaderLibrary shaderLibrary,
            in OutputDescription outputDescription,
            ResourceLayout viewProjectionLayout)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            TextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment
                ),
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment
                )
            ));

            TransformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "World",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                )
            ));

            (Shader vs, Shader fs) = shaderLibrary.LoadShaderSet("cube");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { CubeVertex.LayoutDescription },
                new[] { vs, fs });

            Pipeline = factory.CreateGraphicsPipeline(
                new GraphicsPipelineDescription(
                    BlendStateDescription.SingleOverrideBlend,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleList,
                    shaderSetDesc,
                    new[] { viewProjectionLayout, TextureLayout, TransformLayout },
                    outputDescription
                )
            );

            TransformBuffer = new GpuBuffer<Matrix4x4>(
                graphicsDevice,
                BufferUsage.UniformBuffer,
                Matrix4x4.Identity
            );
        }

        public ResourceLayout TransformLayout { get; }
        public ResourceLayout TextureLayout { get; }
        public Pipeline Pipeline { get; }

        public GpuBuffer<Matrix4x4> TransformBuffer { get; }

        public void Dispose()
        {
            Pipeline.Dispose();
            TransformLayout.Dispose();
            TextureLayout.Dispose();
            TransformBuffer.Dispose();
        }
    }
}
