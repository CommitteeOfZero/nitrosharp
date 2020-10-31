using System;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Veldrid;

#nullable enable

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

        private readonly CommandList _transferCommands;
        private readonly CommandList _drawCommands;

        private readonly RenderTarget _swapchainTarget;
        private readonly Swapchain _mainSwapchain;

        private readonly DrawBatch _offscreenBatch;

        public RenderContext(
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
            GraphicsDevice = graphicsDevice;
            ResourceFactory = graphicsDevice.ResourceFactory;

            _mainSwapchain = swapchain;
            _swapchainTarget = RenderTarget.Swapchain(graphicsDevice, swapchain.Framebuffer);

            _transferCommands = ResourceFactory.CreateCommandList();
            _transferCommands.Name = "Transfer commands";
            _drawCommands = ResourceFactory.CreateCommandList();
            _drawCommands.Name = "Draw commands (primary)";
            SecondaryCommandList = ResourceFactory.CreateCommandList();
            SecondaryCommandList.Name = "Secondary";
            Content = contentManager;
            GlyphRasterizer = glyphRasterizer;
            SystemVariables = systemVariables;
            _shaderLibrary = new ShaderLibrary(graphicsDevice);
            ShaderResources = new ShaderResources(
                graphicsDevice,
                _shaderLibrary,
                _swapchainTarget.OutputDescription,
                _swapchainTarget.ViewProjection.ResourceLayout
            );

            ResourceSetCache = new ResourceSetCache(ResourceFactory);
            TextureCache = new TextureCache(GraphicsDevice);
            WhiteTexture = CreateWhiteTexture();

            Quads = new MeshList<QuadVertex>(
                graphicsDevice,
                new MeshDescription(QuadGeometry.Indices, verticesPerMesh: 4),
                initialCapacity: 512
            );
            IconQuads = new MeshList<IconVertex>(
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

        public MeshList<QuadVertex> Quads { get; }
        public MeshList<IconVertex> IconQuads { get; }
        public MeshList<CubeVertex> Cubes { get; }

        public Size DesignResolution { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ResourceFactory ResourceFactory { get; }

        public CommandList SecondaryCommandList { get; }

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

        public DrawBatch BeginBatch(RenderTarget renderTarget, RgbaFloat? clearColor)
        {
            _offscreenBatch.Begin(SecondaryCommandList, renderTarget, clearColor);
            return _offscreenBatch;
        }

        public void BeginFrame(in FrameStamp frameStamp, bool clear)
        {
            _drawCommands.Begin();
            RgbaFloat? clearColor = clear ? RgbaFloat.Black : (RgbaFloat?)null;
            MainBatch.Begin(_drawCommands, _swapchainTarget, clearColor);

            SecondaryCommandList.Begin();

            Quads.Begin();
            IconQuads.Begin();
            Cubes.Begin();
            TextureCache.BeginFrame(frameStamp);
            ResourceSetCache.BeginFrame(frameStamp);
            Text.BeginFrame();
            _transferCommands.Begin();
        }

        private AnimatedIcons LoadIcons(Configuration config)
        {
            CommandList cl = SecondaryCommandList;
            SecondaryCommandList.Begin();
            var waitLine = Icon.Load(this, config.IconPathPatterns.WaitLine);
            SecondaryCommandList.End();
            GraphicsDevice.SubmitCommands(cl);
            return new AnimatedIcons(waitLine);
        }

        public void ResolveGlyphs()
        {
            Text.ResolveGlyphs();
            TextureCache.EndFrame(_transferCommands);
        }

        public Texture CreateFullscreenTexture()
        {
            Size size = _swapchainTarget.Size;
            var desc = TextureDescription.Texture2D(
                size.Width, size.Height,
                mipLevels: 1, arrayLayers: 1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.Sampled
            );
            return ResourceFactory.CreateTexture(ref desc);
        }

        public void CaptureFramebuffer(Texture dstTexture)
        {
            _drawCommands.CopyTexture(_swapchainTarget.ColorTarget, dstTexture);
        }

        public void EndFrame()
        {
            MainBatch.End();

            SecondaryCommandList.End();
            _drawCommands.End();
            ResourceSetCache.EndFrame();

            Text.EndFrame(_transferCommands);
            Quads.End(_transferCommands);
            IconQuads.End(_transferCommands);
            Cubes.End(_transferCommands);
            _transferCommands.End();

            GraphicsDevice.SubmitCommands(_transferCommands);
            GraphicsDevice.SubmitCommands(SecondaryCommandList);
            GraphicsDevice.SubmitCommands(_drawCommands);
        }

        public void Present()
            => GraphicsDevice.SwapBuffers(_mainSwapchain);

        public Sampler GetSampler(FilterMode filterMode) => filterMode switch
        {
            FilterMode.Linear => GraphicsDevice.LinearSampler,
            FilterMode.Point => GraphicsDevice.PointSampler,
            _ => ThrowHelper.Unreachable<Sampler>()
        };

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

            _transferCommands.Begin();
            _transferCommands.CopyTexture(stagingWhite, texture);
            _transferCommands.End();
            GraphicsDevice.SubmitCommands(_transferCommands);
            stagingWhite.Dispose();
            return texture;
        }

        public void Dispose()
        {
            _transferCommands.Dispose();
            _drawCommands.Dispose();
            SecondaryCommandList.Dispose();
            ShaderResources.Dispose();
            WhiteTexture.Dispose();
            Text.Dispose();
            Quads.Dispose();
            Cubes.Dispose();
            TextureCache.Dispose();
            ResourceSetCache.Dispose();
            _swapchainTarget.Dispose();
            _shaderLibrary.Dispose();
        }
    }
}
