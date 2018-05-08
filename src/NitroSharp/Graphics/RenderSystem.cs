using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Veldrid;
using NitroSharp.Primitives;
using NitroSharp.Graphics.Objects;
using NitroSharp.Utilities;
using NitroSharp.Text;

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private readonly Configuration _config;
        private readonly FontService _fontService;

        private GraphicsDevice _gd;
        private Swapchain _swapchain;
        private CommandList _cl;
        private Canvas _canvas;
        private RgbaTexturePool _texturePool;
        private EffectLibrary _effectLibrary;
        private SharedEffectProperties2D _sharedProps2D;
        private SharedEffectProperties3D _sharedProps3D;
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
            _effectLibrary = new EffectLibrary(_gd);

            _sharedProps2D = new SharedEffectProperties2D(_gd);
            _sharedProps2D.Projection = Matrix4x4.CreateOrthographicOffCenter(
                0, DesignResolution.Width, DesignResolution.Height, 0, 0, -1);

            _sharedProps3D = new SharedEffectProperties3D(_gd);
            _sharedProps3D.View = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);

            _sharedProps3D.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathUtil.PI / 3.0f,
                DesignResolution.Width / DesignResolution.Height,
                0.1f,
                1000.0f);

            _canvas = new Canvas(_gd, _effectLibrary, _sharedProps2D);
            _texturePool = new RgbaTexturePool(_gd);
            _rc = new RenderContext(_gd, _gd.ResourceFactory, _cl, _canvas, _effectLibrary,
                _sharedProps2D, _sharedProps3D, _texturePool, _fontService);

            _rc.MainSwapchain = _swapchain = swapchain;
            _rc.Device = device;

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
            _cl.Begin();

            _cl.SetFramebuffer(_swapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            // TODO: introduce RenderQueues to avoid this hack
            _cube?.Render(_rc);

            _canvas.Begin(_cl, _swapchain.Framebuffer);
            base.Update(deltaMilliseconds);
            _canvas.End();

            _cl.End();

            _gd.SubmitCommands(_cl);
            _gd.WaitForIdle();
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
            if (visual is Cube cube)
            {
                _cube = cube;
                return;
            }

            var transform = visual.Entity.Transform.GetTransformMatrix();
            _canvas.SetTransform(transform);
            visual.Render(_rc);
        }

        public void DestroyDeviceResources()
        {
            foreach (var entity in Entities)
            {
                entity.Visual.DestroyDeviceObjects(_rc);
            }

            _canvas.Dispose();
            _effectLibrary.Dispose();
            _sharedProps2D.Dispose();
            _sharedProps3D.Dispose();
            _texturePool.Dispose();
            _cl.Dispose();
        }

        public void Dispose()
        {
            DestroyDeviceResources();
        }
    }
}
