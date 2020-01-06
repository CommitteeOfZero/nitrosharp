using System;
using System.Diagnostics;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal enum EffectKind : byte
    {
        Blit,
        Grayscale,
        BoxBlur
    }

    internal readonly struct EffectDescription
    {
        public readonly EffectKind EffectKind;
        public readonly byte NumberOfPasses;

        public EffectDescription(EffectKind kind, uint nbPasses = 1u)
            => (EffectKind, NumberOfPasses) = (kind, (byte)nbPasses);

        public override int GetHashCode()
            => HashCode.Combine(EffectKind, NumberOfPasses);
    }

    internal static class EffectPipelines
    {
        public static Pipeline Get(Pipelines pipelines, EffectKind effect)
        {
            return effect switch
            {
                EffectKind.Blit => pipelines.Blit,
                EffectKind.Grayscale => pipelines.Grayscale,
                EffectKind.BoxBlur => pipelines.BoxBlur,
                _ => throw new Exception()
            };
        }
    }

    internal readonly struct SinglePassEffect : IDisposable
    {
        private readonly Pipeline _pipeline;
        private readonly Framebuffer _fb;
        private readonly ResourceSet _rs;
        private readonly bool _ownsFramebuffer;

        public SinglePassEffect(
            ResourceFactory resourceFactory,
            Pipelines pipelines,
            ResourceLayout inputLayout,
            Texture input1,
            Texture? input2,
            Sampler sampler,
            EffectKind effectKind,
            Framebuffer? framebuffer = null)
        {
            var textureDesc = TextureDescription.Texture2D(
                input1.Width, input1.Height, input1.MipLevels, input1.ArrayLayers,
                PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled
            );
            _fb = framebuffer ?? resourceFactory.CreateFramebuffer(
                new FramebufferDescription(
                    depthTarget: null,
                    colorTargets: resourceFactory.CreateTexture(ref textureDesc)
                )
            );
            _rs = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                inputLayout,
                input2 == null
                    ? new BindableResource[]
                    {
                        input1,
                        sampler
                    }
                    : new BindableResource[]
                    {
                        input1,
                        input2,
                        sampler
                    }
            ));
            _pipeline = EffectPipelines.Get(pipelines, effectKind);
            _ownsFramebuffer = framebuffer == null;
        }

        public Texture Apply(CommandList commandList)
        {
            commandList.SetFramebuffer(_fb);
            commandList.SetPipeline(_pipeline);
            commandList.SetGraphicsResourceSet(0, _rs);
            commandList.Draw(4);
            return _fb.ColorTargets[0].Target;
        }

        public void Dispose()
        {
            _rs.Dispose();
            if (_ownsFramebuffer)
            {
                _fb.Dispose();
            }
        }
    }

    internal readonly struct MultipassEffect : IDisposable
    {
        private readonly Pipelines _pipelines;
        private readonly ResourceSet _srcResourceSet;
        private readonly ArrayBuilder<EffectKind> _passes;
        private readonly (Framebuffer fb, ResourceSet rs) _target0;
        private readonly (Framebuffer fb, ResourceSet rs) _target1;

        public MultipassEffect(
            ResourceFactory resourceFactory,
            Pipelines pipelines,
            Texture input1,
            Texture? input2,
            Sampler sampler,
            ReadOnlySpan<EffectDescription> effectChain)
        {
            _pipelines = pipelines;
            _passes = new ArrayBuilder<EffectKind>(initialCapacity: 16);
            foreach (ref readonly EffectDescription effectDesc in effectChain)
            {
                for (int i = 0; i < effectDesc.NumberOfPasses; i++)
                {
                    _passes.Add(effectDesc.EffectKind);
                }
            }

            if (_passes.Count < 2)
            {
                throw new InvalidOperationException($"{nameof(MultipassEffect)} is not meant " +
                    $"to be used for single pass effects."
                );
            }

            _srcResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                pipelines.SimpleEffectInputLayout,
                input2 == null
                    ? new BindableResource[]
                    {
                        input1,
                        sampler
                    }
                    : new BindableResource[]
                    {
                        input1,
                        input2,
                        sampler
                    }
            ));

            (Framebuffer fb, ResourceSet rs) createTarget()
            {
                var textureDesc = TextureDescription.Texture2D(
                    input1.Width, input1.Height, input1.MipLevels, input1.ArrayLayers,
                    PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled
                );
                Framebuffer fb = resourceFactory.CreateFramebuffer(new FramebufferDescription(
                    depthTarget: null,
                    colorTargets: resourceFactory.CreateTexture(ref textureDesc)
                ));
                ResourceSet rs = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                    pipelines.SimpleEffectInputLayout,
                    input2 == null
                    ? new BindableResource[]
                    {
                        fb.ColorTargets[0].Target,
                        sampler
                    }
                    : new BindableResource[]
                    {
                        fb.ColorTargets[0].Target,
                        input2,
                        sampler
                    }
                ));
                return (fb, rs);
            }

            _target0 = createTarget();
            _target1 = createTarget();
        }

        public Texture Apply(CommandList commandList)
        {
            commandList.SetFramebuffer(_target0.fb);
            commandList.SetPipeline(EffectPipelines.Get(_pipelines, _passes[0]));
            commandList.SetGraphicsResourceSet(0, _srcResourceSet);
            commandList.Draw(3);

            Texture? result = null;
            for (int i = 1; i < _passes.Count; i++)
            {
                ((Framebuffer fb, ResourceSet rs) src, (Framebuffer fb, ResourceSet rs) dst) =
                    i % 2 == 0 ? (_target0, _target1) : (_target1, _target0);

                commandList.SetFramebuffer(dst.fb);
                commandList.SetPipeline(EffectPipelines.Get(_pipelines, _passes[i]));
                commandList.SetGraphicsResourceSet(0, src.rs);
                commandList.Draw(3);

                if (i == _passes.Count - 1)
                {
                    result = dst.fb.ColorTargets[0].Target;
                }
            }

            Debug.Assert(result != null);
            return result;
        }

        public void Dispose()
        {
            _srcResourceSet.Dispose();
            _target0.rs.Dispose();
            _target0.fb.Dispose();
            _target1.rs.Dispose();
            _target1.fb.Dispose();
        }
    }
}
