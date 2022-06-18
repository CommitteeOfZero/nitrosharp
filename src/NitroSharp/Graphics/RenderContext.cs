using System;
using System.Collections.Generic;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
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
        private readonly Queue<CommandList> _commandListPool = new();

        private readonly RenderTarget _swapchainTarget;
        private readonly Swapchain _mainSwapchain;

        private readonly DrawBatch _offscreenBatch;

        public ViewProjection OrthoProjection { get; }
        public ViewProjection PerspectiveViewProjection { get; }

        public RenderContext(
            GameWindow window,
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            ContentManager contentManager,
            GlyphRasterizer glyphRasterizer,
            SystemVariableLookup systemVariables)
        {
            DesignResolution = new Size(
                (uint)gameConfiguration.WindowWidth,
                (uint)gameConfiguration.WindowHeight
            );

            Window = window;
            GraphicsDevice = graphicsDevice;
            ResourceFactory = graphicsDevice.ResourceFactory;

            _mainSwapchain = swapchain;
            _swapchainTarget = RenderTarget.Swapchain(graphicsDevice, swapchain.Framebuffer);

            TransferCommands = ResourceFactory.CreateCommandList();
            TransferCommands.Name = "Transfer commands";
            _drawCommands = ResourceFactory.CreateCommandList();
            _drawCommands.Name = "Draw commands (primary)";
            _secondaryCommandList = ResourceFactory.CreateCommandList();
            _secondaryCommandList.Name = "Secondary";
            Content = contentManager;
            GlyphRasterizer = glyphRasterizer;
            SystemVariables = systemVariables;
            _shaderLibrary = new ShaderLibrary(graphicsDevice);

            OrthoProjection = ViewProjection.CreateOrtho(
                graphicsDevice,
                new RectangleF(Vector2.Zero, DesignResolution)
            );

            var view = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 3.0f,
                (float)DesignResolution.Width / DesignResolution.Height,
                0.1f,
                1000.0f
            );
            PerspectiveViewProjection = new ViewProjection(GraphicsDevice, view * projection);

            ShaderResources = new ShaderResources(
                graphicsDevice,
                _shaderLibrary,
                _swapchainTarget.OutputDescription,
                OrthoProjection.ResourceLayout
            );

            ResourceSetCache = new ResourceSetCache(ResourceFactory);
            TextureCache = new TextureCache(GraphicsDevice);
            WhiteTexture = CreateWhiteTexture();

            Quads = new MeshList<QuadVertex>(
                graphicsDevice,
                new MeshDescription(QuadGeometry.Indices, verticesPerMesh: 4),
                initialCapacity: 512
            );
            QuadsUV3 = new MeshList<QuadVertexUV3>(
                graphicsDevice,
                new MeshDescription(QuadGeometry.Indices, verticesPerMesh: 4),
                initialCapacity: 4
            );
            Cubes = new MeshList<CubeVertex>(
                graphicsDevice,
                new MeshDescription(Cube.Indices, verticesPerMesh: 24),
                initialCapacity: 1
            );

            Text = new TextRenderContext(
                GraphicsDevice,
                GlyphRasterizer,
                TextureCache
            );

            MainBatch = new DrawBatch(this);
            _offscreenBatch = new DrawBatch(this);

            Icons = LoadIcons(gameConfiguration);
        }

        public GameWindow Window { get; }
        public MeshList<QuadVertex> Quads { get; }
        public MeshList<QuadVertexUV3> QuadsUV3 { get; }
        public MeshList<CubeVertex> Cubes { get; }

        public Size DesignResolution { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ResourceFactory ResourceFactory { get; }

        public CommandList TransferCommands { get; }

        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public ShaderResources ShaderResources { get; }

        public ResourceSetCache ResourceSetCache { get; }
        public TextureCache TextureCache { get; }
        public Texture WhiteTexture { get; }

        public TextRenderContext Text { get; }

        public DrawBatch MainBatch { get; }

        public AnimatedIcons Icons { get; }

        public SystemVariableLookup SystemVariables { get; }

        public CommandList RentCommandList()
        {
            if (!_commandListPool.TryDequeue(out CommandList? cl))
            {
                cl = ResourceFactory.CreateCommandList();
            }

            return cl;
        }

        public void ReturnCommandList(CommandList cl)
        {
            _commandListPool.Enqueue(cl);
        }

        public DrawBatch BeginBatch(RenderTarget renderTarget, RgbaFloat? clearColor)
        {
            _offscreenBatch.Begin(_secondaryCommandList, renderTarget, clearColor);
            return _offscreenBatch;
        }

        public void BeginFrame(in FrameStamp frameStamp, bool clear)
        {
            _drawCommands.Begin();
            RgbaFloat? clearColor = clear ? RgbaFloat.Black : null;
            MainBatch.Begin(_drawCommands, _swapchainTarget, clearColor);

            _secondaryCommandList.Begin();

            Quads.Begin();
            QuadsUV3.Begin();
            Cubes.Begin();
            TextureCache.BeginFrame(frameStamp);
            ResourceSetCache.BeginFrame(frameStamp);
            Text.BeginFrame();
            TransferCommands.Begin();
        }

        private AnimatedIcons LoadIcons(Configuration config)
        {
            CommandList cl = RentCommandList();
            cl.Begin();
            var waitLine = Icon.Load(this, config.IconPathPatterns.WaitLine);
            cl.End();
            GraphicsDevice.SubmitCommands(cl);
            ReturnCommandList(cl);
            return new AnimatedIcons(waitLine);
        }

        public void ResolveGlyphs()
        {
            Text.ResolveGlyphs();
            TextureCache.EndFrame(TransferCommands);
        }

        public Texture CreateFullscreenTexture(bool staging = false)
        {
            Size size = _swapchainTarget.Size;
            var desc = TextureDescription.Texture2D(
                size.Width, size.Height,
                mipLevels: 1, arrayLayers: 1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                staging ? TextureUsage.Staging : TextureUsage.Sampled
            );
            return ResourceFactory.CreateTexture(ref desc);
        }

        public void CaptureFramebuffer(Texture dstTexture)
        {
            _drawCommands.CopyTexture(_swapchainTarget.ColorTarget, dstTexture);
        }

        public Texture ReadbackTexture(CommandList cl, Texture texture)
        {
            Texture staging = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                texture.Width, texture.Height, texture.MipLevels, texture.ArrayLayers,
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
            TextureCache.EndFrame(TransferCommands);
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
                FilterMode.Linear => GraphicsDevice.LinearSampler,
                FilterMode.Point => GraphicsDevice.PointSampler,
                _ => ThrowHelper.Unreachable<Sampler>()
            };
        }

        private Texture CreateWhiteTexture()
        {
            var textureDesc = TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            );
            Texture stagingWhite = ResourceFactory.CreateTexture(ref textureDesc);
            MappedResourceView<RgbaByte> pixels = GraphicsDevice.Map<RgbaByte>(
                stagingWhite, MapMode.Write
            );
            pixels[0] = RgbaByte.White;
            GraphicsDevice.Unmap(stagingWhite);

            textureDesc.Usage = TextureUsage.Sampled;
            Texture texture = ResourceFactory.CreateTexture(ref textureDesc);

            TransferCommands.Begin();
            TransferCommands.CopyTexture(stagingWhite, texture);
            TransferCommands.End();
            GraphicsDevice.SubmitCommands(TransferCommands);
            stagingWhite.Dispose();
            return texture;
        }

        public void Dispose()
        {
            GraphicsDevice.WaitForIdle();
            OrthoProjection.Dispose();
            Icons.Dispose();
            TransferCommands.Dispose();
            _drawCommands.Dispose();
            _secondaryCommandList.Dispose();
            while (_commandListPool.TryDequeue(out CommandList? cl))
            {
                cl.Dispose();
            }
            ShaderResources.Dispose();
            WhiteTexture.Dispose();
            Text.Dispose();
            Quads.Dispose();
            Cubes.Dispose();
            TextureCache.Dispose();
            ResourceSetCache.Dispose();
            _swapchainTarget.Dispose();
            _shaderLibrary.Dispose();
            _mainSwapchain.Dispose();
            GraphicsDevice.Dispose();
        }
    }
}
