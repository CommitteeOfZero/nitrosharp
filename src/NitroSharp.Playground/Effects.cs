using System;
using System.Diagnostics;
using NitroSharp.Graphics;
using Veldrid;

namespace NitroSharp.Playground
{
    internal enum EffectKind
    {
        Grayscale,
        Blit,
        BoxBlur
    }

    internal class Effects : IDisposable
    {
        private readonly Pipeline _blit;
        private readonly Pipeline _grayscale;
        private readonly Pipeline _boxblur;

        public Effects(ResourceFactory resourceFactory, ShaderLibrary shaderLibrary)
        {
            ResourceLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
              new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
              new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            Pipeline createPipeline(string shaderSet)
            {
                var outputDesc = new OutputDescription(
                    depthAttachment: null,
                    new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm)
                );

                (Shader vs, Shader fs) = shaderLibrary.GetShaderSet(shaderSet);
                return resourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                    BlendStateDescription.SingleOverrideBlend,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleList,
                    new ShaderSetDescription(
                        Array.Empty<VertexLayoutDescription>(),
                        new[] { vs, fs }
                    ),
                    new[] { ResourceLayout },
                    outputDesc
                ));
            }

            _blit = createPipeline("blit");
            _grayscale = createPipeline("grayscalefx");
            _boxblur = createPipeline("boxblur");
        }

        public ResourceLayout ResourceLayout { get; }

        public void Dispose()
        {
            _blit.Dispose();
            _grayscale.Dispose();
            _boxblur.Dispose();
        }

        public Pipeline GetPipeline(EffectKind effect)
        {
            return effect switch
            {
                EffectKind.Blit => _blit,
                EffectKind.Grayscale => _grayscale,
                EffectKind.BoxBlur => _boxblur,
                _ => throw new Exception()
            };
        }
    }

    internal readonly struct SinglePassEffect : IDisposable
    {
        private readonly Pipeline _pipeline;
        private readonly Framebuffer _fb;
        private readonly ResourceSet _rs;

        public SinglePassEffect(
            ResourceFactory resourceFactory,
            Effects effects,
            Texture inputTex,
            Sampler sampler,
            EffectKind effectKind)
        {
            var textureDesc = TextureDescription.Texture2D(
                inputTex.Width, inputTex.Height, inputTex.MipLevels, inputTex.ArrayLayers,
                inputTex.Format, TextureUsage.RenderTarget | TextureUsage.Sampled
            );
            _fb = resourceFactory.CreateFramebuffer(new FramebufferDescription(
                depthTarget: null,
                colorTargets: resourceFactory.CreateTexture(ref textureDesc)
            ));
            _rs = resourceFactory.CreateResourceSet(new ResourceSetDescription(
               effects.ResourceLayout,
               inputTex,
               sampler
            ));
            _pipeline = effects.GetPipeline(effectKind);
        }

        public Texture Apply(CommandList commandList)
        {
            commandList.SetFramebuffer(_fb);
            commandList.SetPipeline(_pipeline);
            commandList.SetGraphicsResourceSet(0, _rs);
            commandList.Draw(3);
            return _fb.ColorTargets[0].Target;
        }

        public void Dispose()
        {
            _rs.Dispose();
            _fb.Dispose();
        }
    }

    internal readonly struct MultipassEffect : IDisposable
    {
        private readonly (Framebuffer fb, ResourceSet rs) _target0;
        private readonly (Framebuffer fb, ResourceSet rs) _target1;
        private readonly Effects _effects;
        private readonly Texture _srcTexture;
        private readonly EffectKind[] _effectChain;

        public MultipassEffect(
            ResourceFactory resourceFactory,
            Effects effects,
            Texture texture,
            Sampler sampler,
            EffectKind[] effectChain)
        {
            _effects = effects;
            _srcTexture = texture;
            _effectChain = effectChain;
            (Framebuffer fb, ResourceSet rs) createTarget()
            {
                var textureDesc = TextureDescription.Texture2D(
                    texture.Width, texture.Height, texture.MipLevels, texture.ArrayLayers,
                    texture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled
                );
                Framebuffer fb = resourceFactory.CreateFramebuffer(new FramebufferDescription(
                    depthTarget: null,
                    colorTargets: resourceFactory.CreateTexture(ref textureDesc)
                ));
                ResourceSet rs = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                    effects.ResourceLayout,
                    fb.ColorTargets[0].Target,
                    sampler
                ));
                return (fb, rs);
            };

            _target0 = createTarget();
            _target1 = createTarget();
        }

        public Texture Apply(CommandList commandList)
        {
            commandList.CopyTexture(_srcTexture, _target0.fb.ColorTargets[0].Target);

            Texture? result = null;
            for (int i = 0; i < _effectChain.Length; i++)
            {
                ((Framebuffer fb, ResourceSet rs) src, (Framebuffer fb, ResourceSet rs) dst) =
                    i % 2 == 0 ? (_target0, _target1) : (_target1, _target0);

                commandList.SetFramebuffer(dst.fb);
                commandList.SetPipeline(_effects.GetPipeline(_effectChain[i]));
                commandList.SetGraphicsResourceSet(0, src.rs);
                commandList.Draw(3);

                if (i == _effectChain.Length - 1)
                {
                    result = dst.fb.ColorTargets[0].Target;
                }
            }

            Debug.Assert(result != null);
            return result;
        }

        public void Dispose()
        {
            _target0.rs.Dispose();
            _target0.fb.Dispose();
            _target1.rs.Dispose();
            _target1.fb.Dispose();
        }
    }
}
