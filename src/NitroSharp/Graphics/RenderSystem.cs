using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.New;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class ViewProjection
    {
        public ViewProjection(ResourceLayout layout, ResourceSet set, DeviceBuffer buffer)
        {
            ResourceLayout = layout;
            ResourceSet = set;
            DeviceBuffer = buffer;
        }

        public ResourceLayout ResourceLayout { get; }
        public ResourceSet ResourceSet { get; }
        public DeviceBuffer DeviceBuffer { get; }
    }

    internal sealed class RenderSystem : IDisposable
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

        private readonly VertexList<QuadVertex> _quadVertexBuffer;
        private DeviceBuffer? _quadIndexBuffer;

        private readonly Texture _whiteTexture;
        private readonly Texture _screenshotTexture;

        private readonly ResourceSetCache _resourceSetCache;
        private ArrayBuilder<BindableResource> _shaderResources;

        private readonly RenderBucket _renderBucket;
        private readonly TextureCache _textureCache;

        private readonly ShaderLibrary _shaderLibrary;

        public RenderSystem(
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
            _renderBucket = new RenderBucket(initialCapacity: 512);
            _textureCache = new TextureCache(_gd);
        }

        public SizeF DesignResolution { get; }

        public void Render(in FrameStamp frameStamp)
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

            _effectCL.End();
            _gd.SubmitCommands(_effectCL);

            _resourceSetCache.EndFrame();
            _textureCache.EndFrame(_cl);
            _quadVertexBuffer.End(_cl);
            _renderBucket.End(default);

            _cl.End();

            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_swapchain);
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
            _cl.Dispose();
            _effectCL.Dispose();
            _pipelines.Dispose();
            _whiteTexture.Dispose();
            _screenshotTexture.Dispose();
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
