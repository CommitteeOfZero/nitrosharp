using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal enum BlendMode : byte
    {
        Alpha,
        Additive,
        ReverseSubtractive,
        Multiplicative
    }

    internal enum FilterMode : byte
    {
        Point,
        Linear
    }

    internal sealed class RenderContext : IDisposable
    {
        private readonly ShaderLibrary _shaderLibrary;

        private readonly CommandList _drawCommands;
        private readonly CommandList _secondaryCommandList;

        private readonly Swapchain _mainSwapchain;
        private readonly RenderTarget _swapchainTarget;
        private readonly DrawBatch _offscreenBatch;

        private readonly TextureCache _textureCache;

        public RenderContext(
            GameWindow window,
            Config config,
            GameProfile gameProfile,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            ContentManager contentManager,
            GlyphRasterizer glyphRasterizer,
            SystemVariableLookup systemVariables)
        {
            DesignResolution = gameProfile.DesignResolution;
            RenderResolution = window.Size;
            Window = window;
            GraphicsDevice = graphicsDevice;
            ResourceFactory = graphicsDevice.ResourceFactory;
            Content = contentManager;
            GlyphRasterizer = glyphRasterizer;
            SystemVariables = systemVariables;
            _mainSwapchain = swapchain;
            _shaderLibrary = new ShaderLibrary(graphicsDevice);

            _swapchainTarget = RenderTarget.Swapchain(graphicsDevice, swapchain.Framebuffer);
            OffscreenTarget = new RenderTarget(graphicsDevice, _swapchainTarget.Size);
            OffscreenTexturePool = new ResourcePool<Texture>(CreateOffscreenTexture, initialSize: 4);

            TransferCommands = ResourceFactory.CreateCommandList();
            TransferCommands.Name = "Transfer commands";
            _drawCommands = ResourceFactory.CreateCommandList();
            _drawCommands.Name = "Draw commands (primary)";
            _secondaryCommandList = ResourceFactory.CreateCommandList();
            _secondaryCommandList.Name = "Secondary";
            CommandListPool = new ResourcePool<CommandList>(ResourceFactory.CreateCommandList, initialSize: 2);

            double scaledDesignArea = Math.Min(
                (double)RenderResolution.Width * DesignResolution.Height,
                (double)RenderResolution.Height * DesignResolution.Width
            );
            double designArea = (double)DesignResolution.Width * DesignResolution.Height;
            double viewportWidth = scaledDesignArea / DesignResolution.Height;
            double viewportHeight = scaledDesignArea / DesignResolution.Width;
            RenderScale = (float)(scaledDesignArea / designArea);
            ViewportLeft = (float)((RenderResolution.Width - viewportWidth) / 2);
            ViewportTop = (float)((RenderResolution.Height - viewportHeight) / 2);
            ViewportRight = (float)((RenderResolution.Width + viewportWidth) / 2);
            ViewportBottom = (float)((RenderResolution.Height + viewportHeight) / 2);

            OrthoProjection = ViewProjection.CreateOrtho(
                graphicsDevice,
                new ScreenRectU(PointU<ScreenPixel>.Zero, RenderResolution)
            );

            PerspectiveViewProjection = ViewProjection.CreatePerspective(
                graphicsDevice,
                fov: MathF.PI / 3,
                (float)RenderResolution.Width / RenderResolution.Height
            );

            ShaderResources = new ShaderResources(
                graphicsDevice,
                _shaderLibrary,
                _swapchainTarget.OutputDescription,
                OrthoProjection.ResourceLayout
            );

            ResourceSetCache = new ResourceSetCache(ResourceFactory);
            _textureCache = new TextureCache(GraphicsDevice);
            WhiteTexture = CreateWhiteTexture();

            Quads = new PrimitiveBatch<QuadVertex>(
                graphicsDevice,
                new PrimitiveTemplate(QuadPrimitive.IndexPattern, QuadPrimitive.VertexCount),
                initialCapacity: 512
            );
            QuadsUV3 = new PrimitiveBatch<QuadVertexUV3>(
                graphicsDevice,
                new PrimitiveTemplate(QuadPrimitive.IndexPattern, QuadPrimitiveUV3.VertexCount),
                initialCapacity: 4
            );
            Cubes = new PrimitiveBatch<CubeVertex>(
                graphicsDevice,
                new PrimitiveTemplate(CubePrimitive.IndexPattern, CubePrimitive.VertexCount),
                initialCapacity: 1
            );

            Text = new TextRenderContext(GraphicsDevice, GlyphRasterizer, _textureCache);
            MainBatch = new DrawBatch(this);
            _offscreenBatch = new DrawBatch(this);

            Icons = LoadIcons(gameProfile);

            CustomSampler = ResourceFactory.CreateSampler(CustomSamplerDesc);
        }

        public Sampler CustomSampler { get; set; }

        public static readonly SamplerDescription CustomSamplerDesc = new()
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
            LodBias = 0,
            MinimumLod = 0,
            MaximumLod = uint.MaxValue,
            MaximumAnisotropy = 0,
        };

        public GameWindow Window { get; }
        public DesignSizeU DesignResolution { get; }
        public ScreenSizeU RenderResolution { get; }
        public ViewProjection OrthoProjection { get; }
        public ViewProjection PerspectiveViewProjection { get; }
        public PrimitiveBatch<QuadVertex> Quads { get; }
        public PrimitiveBatch<QuadVertexUV3> QuadsUV3 { get; }
        public PrimitiveBatch<CubeVertex> Cubes { get; }

        public float RenderScale { get; }
        public float ViewportLeft { get; }
        public float ViewportTop { get; }
        public float ViewportRight { get; }
        public float ViewportBottom { get; }

        public GraphicsDevice GraphicsDevice { get; }
        public ResourceFactory ResourceFactory { get; }

        public CommandList TransferCommands { get; }
        public ResourcePool<CommandList> CommandListPool { get; }

        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public ShaderResources ShaderResources { get; }

        public ResourceSetCache ResourceSetCache { get; }
        public Texture WhiteTexture { get; }

        public TextRenderContext Text { get; }

        public DrawBatch MainBatch { get; }

        public RenderTarget OffscreenTarget { get; }
        public ResourcePool<Texture> OffscreenTexturePool { get; }

        public AnimatedIcons Icons { get; }

        public SystemVariableLookup SystemVariables { get; }

        private Texture CreateWhiteTexture()
        {
            var textureDesc = TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            );
            Texture stagingWhite = ResourceFactory.CreateTexture(in textureDesc);
            MappedResourceView<RgbaByte> pixels = GraphicsDevice.Map<RgbaByte>(
                stagingWhite, MapMode.Write
            );
            pixels[0] = RgbaByte.White;
            GraphicsDevice.Unmap(stagingWhite);

            textureDesc.Usage = TextureUsage.Sampled;
            Texture texture = ResourceFactory.CreateTexture(in textureDesc);

            TransferCommands.Begin();
            TransferCommands.CopyTexture(stagingWhite, texture);
            TransferCommands.End();
            GraphicsDevice.SubmitCommands(TransferCommands);
            stagingWhite.Dispose();
            return texture;
        }

        private Texture CreateOffscreenTexture()
        {
            ScreenSizeU size = _swapchainTarget.Size;
            var desc = TextureDescription.Texture2D(
                size.Width, size.Height,
                mipLevels: 1, arrayLayers: 1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.Sampled
            );
            return ResourceFactory.CreateTexture(in desc);
        }

        private AnimatedIcons LoadIcons(GameProfile gameProfile)
        {
            Icon? waitLine = null;
            if (Icon.Exists(Content, gameProfile.IconPathPatterns.WaitLine))
            {
                waitLine = Icon.Load(this, gameProfile.IconPathPatterns.WaitLine);
            }
            return new AnimatedIcons(waitLine);
        }

        public void BeginFrame(in FrameStamp frameStamp)
        {
            BeginFrame(frameStamp, _swapchainTarget);
        }

        public void BeginFrame(in FrameStamp frameStamp, RenderTarget renderTarget)
        {
            _drawCommands.Begin();
            MainBatch.Begin(_drawCommands, renderTarget);

            _secondaryCommandList.Begin();

            Quads.Begin();
            QuadsUV3.Begin();
            Cubes.Begin();
            _textureCache.BeginFrame(frameStamp);
            ResourceSetCache.BeginFrame(frameStamp);
            Text.BeginFrame();
            TransferCommands.Begin();
        }

        public DrawBatch BeginOffscreenBatch(RenderTarget renderTarget)
        {
            _offscreenBatch.Begin(_secondaryCommandList, renderTarget);
            return _offscreenBatch;
        }

        public void ResolveGlyphs()
        {
            Text.ResolveGlyphs();
            _textureCache.EndFrame(TransferCommands);
        }

        public Texture ReadbackTexture(CommandList cl, Texture texture)
        {
            Texture staging = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                texture.Width, texture.Height,texture.MipLevels, texture.ArrayLayers,
                texture.Format, TextureUsage.Staging
            ));

            cl.CopyTexture(texture, staging);
            return staging;
        }

        public void EndFrame()
        {
            MainBatch.End();

            _secondaryCommandList.End();
            _drawCommands.End();
            ResourceSetCache.EndFrame();

            Text.EndFrame(TransferCommands);
            if (_textureCache.IsActive)
            {
                _textureCache.EndFrame(TransferCommands);
            }
            Quads.End(TransferCommands);
            QuadsUV3.End(TransferCommands);
            Cubes.End(TransferCommands);
            TransferCommands.End();

            GraphicsDevice.SubmitCommands(TransferCommands);
            GraphicsDevice.SubmitCommands(_secondaryCommandList);
            GraphicsDevice.SubmitCommands(_drawCommands);
        }

        public void Present()
        {
            GraphicsDevice.SwapBuffers(_mainSwapchain);
        }

        public Sampler GetSampler(FilterMode filterMode)
        {
            return filterMode switch
            {
                FilterMode.Linear => CustomSampler,
                FilterMode.Point => GraphicsDevice.PointSampler,
                _ => throw ThrowHelper.UnexpectedValueOf<FilterMode>()
            };
        }

        public Matrix4x4 GetTransformMatrix(Transform transform, DesignSize size, bool useScaling, bool aligned)
        {
            var bounds = size.ToVector2();
            var center = new Vector3(new Vector2(0.5f) * bounds, 0);
            var scale = Matrix4x4.CreateScale(transform.Scale, center);
            Matrix4x4 rot = Matrix4x4.CreateRotationZ(rad(transform.Rotation.Z), center)
                * Matrix4x4.CreateRotationY(rad(transform.Rotation.Y), center)
                * Matrix4x4.CreateRotationX(rad(transform.Rotation.X), center);
            Vector3 finalPosition = new Vector3(ViewportLeft, ViewportTop, 0) + transform.Position * RenderScale;
            if (aligned)
            {
                finalPosition = new Vector3(
                    (float)Math.Floor(finalPosition.X),
                    (float)Math.Floor(finalPosition.Y),
                    (float)Math.Floor(finalPosition.Z)
                );
            }
            var translation = Matrix4x4.CreateTranslation(finalPosition);
            Matrix4x4 matrix = scale * rot * translation;
            if (useScaling)
            {
                matrix = Matrix4x4.CreateScale(RenderScale) * matrix;
            }
            return matrix;

            static float rad(float deg) => deg / 180.0f * MathF.PI;
        }

        public void Dispose()
        {
            GraphicsDevice.WaitForIdle();
            OrthoProjection.Dispose();
            Icons.Dispose();
            TransferCommands.Dispose();
            _drawCommands.Dispose();
            _secondaryCommandList.Dispose();
            CommandListPool.Dispose();
            ShaderResources.Dispose();
            WhiteTexture.Dispose();
            Text.Dispose();
            Quads.Dispose();
            Cubes.Dispose();
            _textureCache.Dispose();
            ResourceSetCache.Dispose();
            _swapchainTarget.Dispose();
            OffscreenTarget.Dispose();
            OffscreenTexturePool.Dispose();
            _shaderLibrary.Dispose();
            _mainSwapchain.Dispose();
            GraphicsDevice.Dispose();
        }
    }
}
