using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Veldrid;
using NitroSharp.Primitives;
using NitroSharp.Graphics.Objects;
using NitroSharp.Text;
using NitroSharp.Utilities;

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private readonly Configuration _config;
        private readonly FontService _fontService;

        private GraphicsDevice _gd;
        private Swapchain _swapchain;
        private CommandList _cl;
        private PrimitiveBatcher _primitiveBatch;
        private RgbaTexturePool _texturePool;
        private RenderContext _rc;

        private Cube _cube;

        public RenderSystem(Configuration configuration, FontService fontService)
        {
            _config = configuration;
            _fontService = fontService;
        }

        public void CreateDeviceResources(GraphicsDevice device, Swapchain swapchain)
        {
            _gd = device;
            ResourceFactory factory = _gd.ResourceFactory;
            _cl = factory.CreateCommandList();
            _cl.Name = "Main";

            ResourceLayout sharedResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
                0, DesignResolution.Width, DesignResolution.Height, 0, 0, -1);

            DeviceBuffer projectionBuffer = _gd.CreateStaticBuffer(
                ref projection, BufferUsage.UniformBuffer);

            ResourceSet sharedResourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(sharedResourceLayout, projectionBuffer));

            var sharedConstants = new SharedResources(
                sharedResourceLayout,
                projectionBuffer,
                sharedResourceSet);

            var mainBucket = new RenderBucket(_gd, 512);

            var quadGeometryStream = new QuadGeometryStream(device);
            var cache = new ResourceSetCache(_gd.ResourceFactory);
            var shaderLibrary = new ShaderLibrary(_gd);
            _primitiveBatch = new PrimitiveBatcher(_gd, mainBucket, quadGeometryStream, shaderLibrary, sharedConstants, cache, swapchain.Framebuffer);
            _texturePool = new RgbaTexturePool(_gd);
            _rc = new RenderContext(_gd, _gd.ResourceFactory, _cl, _primitiveBatch, _texturePool, _fontService);

            _rc.Device = device;
            _rc.MainSwapchain = _swapchain = swapchain;
            _rc.DesignResolution = new Size((uint)DesignResolution.Width, (uint)DesignResolution.Height);
            _rc.SharedConstants = sharedConstants;
            _rc.QuadGeometryStream = quadGeometryStream;
            _rc.ShaderLibrary = shaderLibrary;

            _rc.MainBucket = mainBucket;

            foreach (var entity in Entities)
            {
                entity.Visual.CreateDeviceObjects(_rc);
            }
        }

        public RenderContext RenderContext => _rc;
        private SizeF DesignResolution => new SizeF(_config.WindowWidth, _config.WindowHeight);

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var visual = entity.Visual;
            if (!visual.IsInitialized)
            {
                visual.CreateDeviceObjects(_rc);
                visual.IsInitialized = true;
            }
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            entity.Visual.DestroyDeviceObjects(_rc);
        }

        public override void Update(float deltaMilliseconds)
        {
            _rc.QuadGeometryStream.Begin();

            _cl.Begin();

            _cl.SetFramebuffer(_swapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            _rc.MainBucket.Begin();
            base.Update(deltaMilliseconds);

            _rc.QuadGeometryStream.End(_cl);
            _rc.MainBucket.End(_cl);

            _cl.End();

            _gd.SubmitCommands(_cl);
        }

        public void Present()
        {
            _gd.SwapBuffers(_swapchain);
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority).ThenBy(x => x.CreationTime);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visual = entity.Visual;
            if (visual.IsEnabled)
            {
                RenderItem(visual, Vector2.One);
            }
        }

        private void RenderItem(Visual visual, Vector2 scale)
        {
            if (visual.Priority == 0)
            {
                return;
            }

            var transform = visual.Entity.Transform.GetTransformMatrix();
            _primitiveBatch.SetTransform(transform);
            visual.Render(_rc);
        }

        public void DestroyDeviceResources()
        {
            foreach (var entity in Entities)
            {
                entity.Visual.DestroyDeviceObjects(_rc);
            }

            _primitiveBatch.Dispose();
            _texturePool.Dispose();
            _cl.Dispose();
        }

        public void Dispose()
        {
            DestroyDeviceResources();
        }
    }
}
