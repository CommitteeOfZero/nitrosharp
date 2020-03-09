using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Content;
using NitroSharp.Experimental;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Old
{
    internal readonly struct RenderContext
    {
        public readonly ContentManager Content;
        public readonly GraphicsDevice GraphicsDevice;
        public readonly Pipelines Pipelines;
        public readonly CommandList CommandList;
        public readonly Framebuffer PrimaryFramebuffer;
        public readonly Framebuffer SecondaryFramebuffer;

        public RenderContext(
            ContentManager content,
            GraphicsDevice graphicsDevice,
            Pipelines pipelines,
            CommandList commandList,
            Framebuffer primaryFramebuffer,
            Framebuffer secondaryFramebuffer)
        {
            Content = content;
            GraphicsDevice = graphicsDevice;
            Pipelines = pipelines;
            CommandList = commandList;
            PrimaryFramebuffer = primaryFramebuffer;
            SecondaryFramebuffer = secondaryFramebuffer;
        }
    }

    internal struct DrawState
    {
        public Pipeline Pipeline;
        public ResourceSet ResourceSet0;
        public ResourceSet? ResourceSet1;
        public UniformUpdate UniformUpdate;
        public Action<RenderContext>? Callback;
        public int MaterialHash;
    }

    internal sealed class Renderer : IDisposable
    {
        private readonly World _world;
        private readonly GraphicsDevice _gd;
        private readonly Swapchain _swapchain;
        private readonly ResourceFactory _rf;

        private Framebuffer? _targetFramebuffer;
        private readonly Framebuffer _swapchainFramebuffer;
        private readonly Framebuffer _secondaryFramebuffer;
        private readonly Texture _secondaryFramebufferTexture;
        private readonly ResourceLayout _viewProjectionLayout;
        private readonly ResourceSet _viewProjectionSet;
        private readonly DeviceBuffer _viewProjectionBuffer;
        private readonly Pipelines _pipelines;
        private readonly CommandList _cl;
        private readonly CommandList _effectCL;
        private readonly RenderContext _renderContext;

        private readonly VertexList<QuadVertex> _quadVertexBuffer;
        private DeviceBuffer? _quadIndexBuffer;

        private readonly Texture _whiteTexture;
        private readonly Texture _screenshotTexture;

        private readonly DeviceBuffer _transitionParamUB;
        private readonly ResourceSet _transitionParamSet;
        private readonly ResourceSetCache _resourceSetCache;
        private ArrayBuilder<BindableResource> _shaderResources;

        private readonly RenderBucket<RenderItemKey> _renderBucket;
        private readonly TextureCache _textureCache;
        private readonly TextRenderer _textRenderer;

        public SizeF DesignResolution { get; }

        private readonly ShaderLibrary _shaderLibrary;

        public Renderer(
            World world,
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            GlyphRasterizer glyphRasterizer,
            ContentManager contentManager)
        {
            _world = world;
            _gd = graphicsDevice;
            _swapchain = swapchain;
            _swapchainFramebuffer = swapchain.Framebuffer;
            _rf = graphicsDevice.ResourceFactory;

            DesignResolution = new SizeF(
                gameConfiguration.WindowWidth,
                gameConfiguration.WindowHeight
            );

            _shaderLibrary = new ShaderLibrary(_gd);
            _viewProjectionLayout = _rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                )
            ));
            var projection = Matrix4x4.CreateOrthographicOffCenter(
                left: 0, right: DesignResolution.Width,
                bottom: DesignResolution.Height, top: 0,
                zNearPlane: 0.0f, zFarPlane: -1.0f
            );
            _viewProjectionBuffer = _gd.CreateStaticBuffer(ref projection, BufferUsage.UniformBuffer);
            _viewProjectionSet = _rf.CreateResourceSet(
                new ResourceSetDescription(
                    _viewProjectionLayout,
                    _viewProjectionBuffer
                )
            );
            var viewProjection = new ViewProjection(
                _viewProjectionLayout,
                _viewProjectionSet,
                _viewProjectionBuffer
            );

            _pipelines = new Pipelines(
                _rf,
                _shaderLibrary,
                _swapchainFramebuffer.OutputDescription,
                viewProjection
            );
            _cl = _rf.CreateCommandList();
            _effectCL = _rf.CreateCommandList();
            _whiteTexture = CreateWhiteTexture();
            _screenshotTexture = _rf.CreateTexture(TextureDescription.Texture2D(
                _swapchainFramebuffer.Width, _swapchainFramebuffer.Height, mipLevels: 1,
                arrayLayers: 1, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled
            ));

            _secondaryFramebufferTexture = _rf.CreateTexture(TextureDescription.Texture2D(
                _swapchainFramebuffer.Width, _swapchainFramebuffer.Height, mipLevels: 1,
                arrayLayers: 1, PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled
            ));
            _secondaryFramebuffer = _rf.CreateFramebuffer(new FramebufferDescription(
                depthTarget: null,
                _secondaryFramebufferTexture
            ));

            _quadVertexBuffer = new VertexList<QuadVertex>(_gd, initialCapacity: 512 * 4);
            ResizeIndexBufferIfNecessary();
            _resourceSetCache = new ResourceSetCache(_rf);
            _shaderResources = new ArrayBuilder<BindableResource>(4);
            _renderBucket = new RenderBucket<RenderItemKey>(initialCapacity: 512);
            _textureCache = new TextureCache(_gd);
            _textRenderer = new TextRenderer(
                world,
                _gd,
                _swapchainFramebuffer.OutputDescription,
                _shaderLibrary,
                viewProjection,
                glyphRasterizer,
                _textureCache
            );

            _transitionParamUB = _rf.CreateBuffer(new BufferDescription(
                sizeInBytes: 16,
                BufferUsage.UniformBuffer | BufferUsage.Dynamic)
            );
            _transitionParamSet = _rf.CreateResourceSet(new ResourceSetDescription(
                _pipelines.TransitionParamLayout,
                _transitionParamUB
            ));

            _renderContext = new RenderContext(
               contentManager, _gd, _pipelines, _cl,
               swapchain.Framebuffer, _secondaryFramebuffer
           );
        }

        private Texture CreateWhiteTexture()
        {
            var textureDesc = TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            );
            Texture stagingWhite = _rf.CreateTexture(ref textureDesc);
            MappedResourceView<RgbaByte> pixels = _gd.Map<RgbaByte>(
                stagingWhite, MapMode.Write
            );
            pixels[0] = RgbaByte.White;
            _gd.Unmap(stagingWhite);

            textureDesc.Usage = TextureUsage.Sampled;
            Texture texture = _rf.CreateTexture(ref textureDesc);

            _cl.Begin();
            _cl.CopyTexture(stagingWhite, texture);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.DisposeWhenIdle(stagingWhite);
            return texture;
        }


        private readonly Stopwatch _sw = new Stopwatch();

        public void ProcessTransforms()
        {
            QuadStorage quads = _world.Quads.Active;
            TransformProcessor.ProcessTransforms(_world, _world.AlphaMasks.Active);
            TransformProcessor.ProcessTransforms(_world, quads);
            TransformProcessor.ProcessTransforms(_world, _world.Quads.Inactive);
            CalcVertices(quads);
            CalcVertices(_world.Quads.Inactive);
        }

        public void Render(
            in FrameStamp frameStamp,
            ContentManager content,
            bool captureFramebuffer)
        {
            _effectCL.Begin();
            _targetFramebuffer = _swapchainFramebuffer;
            _cl.Begin();
            _cl.SetFramebuffer(_targetFramebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            _renderBucket.Begin();
            _quadVertexBuffer.Begin();
            _textureCache.BeginFrame(frameStamp);
            _resourceSetCache.BeginFrame(frameStamp);
            _textRenderer.BeginFrame();

            QuadStorage quads = _world.Quads.Active;
            for (uint i = 0; i < quads.Count; i++)
            {
                SetDrawState(
                    ref quads.Materials[i],
                    ref quads.DrawState[i],
                    content
                );
            }
            BatchQuads(
                quads.Keys.All,
                quads.DrawState.All,
                quads.Geometry.All
            );

            _effectCL.End();
            _gd.SubmitCommands(_effectCL);

            _textRenderer.ResolveGlyphs();

            _resourceSetCache.EndFrame();
            _textureCache.EndFrame(_cl);
            _textRenderer.EndFrame(_renderBucket, _cl);
            _quadVertexBuffer.End(_cl);
            _renderBucket.End(_renderContext);

            if (captureFramebuffer)
            {
                _cl.CopyTexture(_targetFramebuffer.ColorTargets[0].Target, _screenshotTexture);
            }

            _cl.End();

            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_swapchain);
        }

        private void SetDrawState(
            ref Material material,
            ref DrawState drawState,
            ContentManager content)
        {
            Debug.Assert(_targetFramebuffer != null);
            if (material.TransitionParameters.MaskHandle.NormalizedPath != null)
            {
                drawState.UniformUpdate = _renderBucket.StoreUniformUpdate(
                    _transitionParamUB,
                    ref material.TransitionParameters.FadeAmount
                );
            }

            //if (material.GetHashCode() == drawState.MaterialHash)
            //{
            //    return;
            //}

            Sampler sampler = material.UseLinearFiltering
               ? _gd.LinearSampler
               : _gd.PointSampler;

            Texture source = material.Kind switch
            {
                MaterialKind.SolidColor => _whiteTexture,
                MaterialKind.Texture => content.TryGetTexture(material.TextureVariant.TextureHandle)!,
                MaterialKind.Screenshot => _screenshotTexture,
                MaterialKind.Lens => content.TryGetTexture(material.LensTextureHandle)!
            };

            if (material.Kind == MaterialKind.Lens)
            {

                drawState.Pipeline = _pipelines.BarrelDistortion;
                drawState.ResourceSet0 = _resourceSetCache.GetResourceSet(new ResourceSetKey(
                    _pipelines.BarrelDistortionInputLayout,
                    ShaderResources(_secondaryFramebufferTexture, source, sampler)
                ));
                drawState.Callback = ctx =>
                {
                    ctx.CommandList.CopyTexture(
                        source: ctx.PrimaryFramebuffer.ColorTargets[0].Target,
                        destination: ctx.SecondaryFramebuffer.ColorTargets[0].Target
                    );
                };
            }

            Texture alphaMask = _whiteTexture;
            if (material.AlphaMask.NormalizedPath != null)
            {
                alphaMask = content.TryGetTexture(material.AlphaMask)!;
            }

            Texture input = source;
            if (material.Effects.Count > 0)
            {
                input = applyEffects(input, material.Effects.AsSpan(), sampler);
            }

            if (material.TransitionParameters.MaskHandle.NormalizedPath != null)
            {
                TransitionParameters par = material.TransitionParameters;
                Texture mask = content.TryGetTexture(par.MaskHandle)!;
                drawState.Pipeline = _pipelines.Transition;
                drawState.ResourceSet0 = _resourceSetCache.GetResourceSet(new ResourceSetKey(
                    _pipelines.TransitionInputLayout,
                    ShaderResources(input, mask, sampler)
                ));
                drawState.ResourceSet1 = _transitionParamSet;
            }
            else if (material.Kind != MaterialKind.Lens)
            {
                drawState.Pipeline = material.BlendMode switch
                {
                    BlendMode.Alpha => _pipelines.AlphaBlend,
                    BlendMode.Additive => _pipelines.AdditiveBlend,
                    BlendMode.Multiplicative => _pipelines.MultiplicativeBlend,
                    BlendMode.ReverseSubtractive => _pipelines.ReverseSubtractiveBlend
                };

                drawState.ResourceSet0 = _resourceSetCache.GetResourceSet(new ResourceSetKey(
                    _pipelines.CommonResourceLayout,
                    ShaderResources(input, alphaMask, sampler)
                ));
            }

            drawState.MaterialHash = material.GetHashCode();

            Texture applyEffects(Texture input, ReadOnlySpan<EffectDescription> effects, Sampler sampler)
            {
                if (effects.Length == 1 && effects[0].NumberOfPasses == 1)
                {
                    using var effect = new SinglePassEffect(
                        _rf, _pipelines, _pipelines.SimpleEffectInputLayout,
                        input, input2: null, sampler,
                        effects[0].EffectKind
                    );
                    return effect.Apply(_effectCL);
                }
                else
                {
                    using var effect = new MultipassEffect(
                        _rf, _pipelines,
                        input, input2: null, sampler,
                        effects
                    );
                    return effect.Apply(_effectCL);
                }
            }
        }

        private ReadOnlySpan<BindableResource> ShaderResources(
            BindableResource res1,
            BindableResource res2,
            BindableResource res3)
        {
            _shaderResources.Clear();
            _shaderResources.Add(res1);
            _shaderResources.Add(res2);
            _shaderResources.Add(res3);
            return _shaderResources.AsReadonlySpan();
        }

        private void CalcVertices(QuadStorage quads)
        {
            for (uint i = 0; i < quads.Count; i++)
            {
                ref readonly Material mat = ref quads.Materials[i];
                QuadRenderer.CalcVertices(
                    ref quads.Geometry[i],
                    quads.LocalBounds[i],
                    quads.Transforms[i],
                    mat.UvTopLeft,
                    mat.UvBottomRight,
                    mat.Color.ToVector4(),
                    out quads.DesignSpaceRects[i]
                );
            }
        }

        public void BatchQuads(
            ReadOnlySpan<RenderItemKey> keys,
            ReadOnlySpan<DrawState> drawState,
            Span<QuadGeometry> geometry)
        {
            ResourceSet commonResourceSet = _viewProjectionSet;
            int count = keys.Length;
            uint quadCount = _quadVertexBuffer.Count / 4u;
            for (int i = 0; i < count; i++)
            {
                Span<QuadVertex> vertices = _quadVertexBuffer.Append(4);
                Span<QuadVertex> src = MemoryMarshal.CreateSpan(ref geometry[i].TopLeft, 4);
                src.CopyTo(vertices);
            }

            var multiSub = _renderBucket.PrepareMultiSubmission((uint)count);
            for (int i = 0; i < count; i++)
            {
                multiSub.Keys[i] = keys[i];
                ref readonly DrawState ds = ref drawState[i];
                multiSub.Submissions[i] = new RenderBucketSubmission
                {
                    VertexBuffer0 = _quadVertexBuffer,
                    Pipeline = ds.Pipeline,
                    SharedResourceSet = commonResourceSet,
                    ObjectResourceSet0 = ds.ResourceSet0,
                    ObjectResourceSet1 = ds.ResourceSet1,
                    InstanceCount = 1,
                    IndexBuffer = _quadIndexBuffer,
                    IndexBase = (ushort)(6 * (quadCount + i)),
                    IndexCount = 6,
                    VertexCount = 6,
                    //VertexBase = (ushort)(4 * (quadCount + i)),
                    UniformUpdate = ds.UniformUpdate,
                    BeforeRenderCallback = ds.Callback
                };
            }

            _renderBucket.Submit(multiSub);
        }

        private void ResizeIndexBufferIfNecessary()
        {
            Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 2, 1, 3 };
            uint indicesNeeded = 6 * (_quadVertexBuffer.Capacity / 4u);
            uint requiredSizeInBytes = indicesNeeded * sizeof(ushort);
            if (_quadIndexBuffer == null || _quadIndexBuffer.SizeInBytes != requiredSizeInBytes)
            {
                _quadIndexBuffer?.Dispose();
                _quadIndexBuffer = _rf.CreateBuffer(new BufferDescription(
                    requiredSizeInBytes,
                    BufferUsage.IndexBuffer
                ));

                var indices = new ushort[indicesNeeded];
                for (int i = 0; i < indicesNeeded; i++)
                {
                    int quad = i / 6;
                    int vertexInQuad = i % 6;
                    indices[i] = (ushort)(quadIndices[vertexInQuad] + 4 * quad);
                }
                _gd.UpdateBuffer(_quadIndexBuffer, 0, indices);
            }
        }

        public void Dispose()
        {
            _gd.WaitForIdle();
            _textRenderer.Dispose();
            _cl.Dispose();
            _effectCL.Dispose();
            _pipelines.Dispose();
            _whiteTexture.Dispose();
            _screenshotTexture.Dispose();
            _transitionParamUB.Dispose();
            _transitionParamSet.Dispose();
            _quadVertexBuffer.Dispose();
            _quadIndexBuffer?.Dispose();
            _textureCache.Dispose();
            _resourceSetCache.Dispose();
            _viewProjectionSet.Dispose();
            _viewProjectionLayout.Dispose();
            _viewProjectionBuffer.Dispose();
            _secondaryFramebuffer.Dispose();
            _secondaryFramebufferTexture.Dispose();
            _shaderLibrary.Dispose();
        }
    }
}
