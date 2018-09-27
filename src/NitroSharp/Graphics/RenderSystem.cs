using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Renderers;
using NitroSharp.Media;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Systems
{
    internal sealed class RenderSystem : GameSystem, IDisposable
    {
        private const ushort MainBucketSize = 512;

        private readonly World _world;
        private readonly GraphicsDevice _gd;
        private readonly Swapchain _swapchain;
        private readonly ContentManager _content;
        private readonly Configuration _config;
        private readonly FontService _fontService;

        private readonly CommandList _cl;
        private readonly RenderContext _context;

        private readonly ResourceLayout _viewProjectionLayout;
        private readonly ResourceSet _viewProjectionSet;
        private readonly DeviceBuffer _viewProjectionBuffer;

        private readonly Texture _whiteTexture;
        private readonly TextureView _whiteTextureView;
        private readonly RenderBucket _mainBucket;
        private readonly QuadGeometryStream _quadGeometryStream;
        private readonly QuadBatcher _quadBatcher;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly RectangleRenderer _quadRenderer;
        private readonly TextRenderer _textRenderer;
        private readonly VideoRenderer _videoRenderer;
        private readonly ResourceSetCache _resourceSetCache;
        private readonly ShaderLibrary _shaderLibrary;
        private readonly RgbaTexturePool _texturePool;

        public RenderSystem(World world,
            GraphicsDevice device,
            Swapchain swapchain,
            ContentManager content,
            FontService fontService,
            Configuration gameConfiguration)
        {
            _world = world;
            _gd = device;
            _swapchain = swapchain;
            _content = content;
            _fontService = fontService;
            _config = gameConfiguration;

            DesignResolution = new SizeF(_config.WindowWidth, _config.WindowHeight);

            ResourceFactory factory = _gd.ResourceFactory;
            _cl = factory.CreateCommandList();
            _cl.Name = "Main Pass";

            _resourceSetCache = new ResourceSetCache(_gd.ResourceFactory);
            _shaderLibrary = new ShaderLibrary(_gd);
            _texturePool = new RgbaTexturePool(_gd);

            _viewProjectionLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
                0, DesignResolution.Width, DesignResolution.Height, 0, 0, -1);

            _viewProjectionBuffer = _gd.CreateStaticBuffer(ref projection, BufferUsage.UniformBuffer);
            _viewProjectionSet = factory.CreateResourceSet(new ResourceSetDescription(_viewProjectionLayout, _viewProjectionBuffer));
            var viewProjection = new ViewProjection(_viewProjectionLayout, _viewProjectionSet, _viewProjectionBuffer);

            _mainBucket = new RenderBucket(_gd, MainBucketSize);
            _quadGeometryStream = new QuadGeometryStream(device);
          
            CreateWhiteTexture(out _whiteTexture, out _whiteTextureView);

            _context = new RenderContext
            {
                Device = device,
                ResourceFactory = factory,
                MainSwapchain = swapchain,
                MainFramebuffer = swapchain.Framebuffer,
                MainCommandList = _cl,
                ShaderLibrary = _shaderLibrary,
                TexturePool = _texturePool,
                ResourceSetCache = _resourceSetCache,
                FontService = fontService,
                ViewProjection = viewProjection,
                MainBucket = _mainBucket,
                QuadGeometryStream = _quadGeometryStream,
                WhiteTexture = _whiteTextureView,
                DesignResolution = new Size((uint)DesignResolution.Width, (uint)DesignResolution.Height)
            };

            _context.QuadBatcher = _context.CreateQuadBatcher(_mainBucket, _swapchain.Framebuffer);
            _quadBatcher = _context.QuadBatcher;

            _spriteRenderer = new SpriteRenderer(world, _context, _content);
            _quadRenderer = new RectangleRenderer(world, _context);
            _textRenderer = new TextRenderer(world, _context);

            _videoRenderer = new VideoRenderer(world, _context, _content);
        }

        private SizeF DesignResolution { get; }
        public RenderContext Context => _context;

        private void CreateWhiteTexture(out Texture texture, out TextureView textureView)
        {
            Texture stagingWhite = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            MappedResourceView<RgbaByte> pixels = _gd.Map<RgbaByte>(stagingWhite, MapMode.Write);
            pixels[0] = RgbaByte.White;
            _gd.Unmap(stagingWhite);

            texture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                 width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            textureView = _gd.ResourceFactory.CreateTextureView(texture);

            _cl.Begin();
            _cl.CopyTexture(stagingWhite, texture);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.DisposeWhenIdle(stagingWhite);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            _cl.Begin();
            _cl.SetFramebuffer(_swapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            _mainBucket.Begin();
            _quadGeometryStream.Begin();

            _spriteRenderer.ProcessSprites();
            _quadRenderer.ProcessRectangles(_world.Rectangles);
            _textRenderer.ProcessTextLayouts();
            _videoRenderer.ProcessVideoClips();

            _quadGeometryStream.End(_cl);
            _mainBucket.End(_cl);

            _cl.End();
            _gd.SubmitCommands(_cl);
        }

        public void Present()
        {
            _gd.SwapBuffers(_swapchain);
        }

        public void Dispose()
        {
            _textRenderer.Dispose();
            _videoRenderer.Dispose();

            _quadBatcher.Dispose();
            _quadGeometryStream.Dispose();
            _resourceSetCache.Dispose();
            _texturePool.Dispose();
            _shaderLibrary.Dispose();
            _whiteTextureView.Dispose();
            _whiteTexture.Dispose();

            _viewProjectionSet.Dispose();
            _viewProjectionBuffer.Dispose();
            _viewProjectionLayout.Dispose();

            _cl.Dispose();
        }
    }
}
