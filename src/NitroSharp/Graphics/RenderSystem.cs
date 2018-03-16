using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Veldrid;
using NitroSharp.Primitives;
using NitroSharp.Graphics.Objects;

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private readonly NewNitroConfiguration _config;
        private readonly CommandList _cl;
        private readonly GraphicsDevice _gd;
        private readonly ResourceFactory _factory;
        private readonly Canvas _canvas;
        private readonly EffectLibrary _effectLibrary;
        public readonly RenderContext RC;

        private readonly SharedEffectProperties2D _projectionMatrix2D;
        private readonly SharedEffectProperties3D _camera;
        private Cube _cube;

        public RenderSystem(GraphicsDevice graphicsDevice, NewNitroConfiguration configuration)
        {
            _gd = graphicsDevice;
            _config = configuration;
            
            _factory = _gd.ResourceFactory;
            _cl = _factory.CreateCommandList();
            _effectLibrary = new EffectLibrary(_gd);

            _projectionMatrix2D = new SharedEffectProperties2D(_gd);
            _projectionMatrix2D.Projection = Matrix4x4.CreateOrthographicOffCenter(
                0, DesignResolution.Width, DesignResolution.Height, 0, 0, -1);

            _camera = new SharedEffectProperties3D(_gd);
            _camera.View = Matrix4x4.CreateLookAt(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);
            _camera.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                (float)Math.PI / 3.0f,
                DesignResolution.Width / DesignResolution.Height,
                0.1f,
                1.0f);

            _canvas = new Canvas(graphicsDevice, _effectLibrary, _projectionMatrix2D);
            RC = new RenderContext(_gd, _factory, _cl, _canvas, _effectLibrary, _projectionMatrix2D, _camera);
        }

        private SizeF DesignResolution => new SizeF(_config.WindowWidth, _config.WindowHeight);

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void Update(float deltaMilliseconds)
        {
            _cl.Begin();

            _cl.SetFramebuffer(_gd.SwapchainFramebuffer);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

            _cube?.Render(RC);

            _canvas.Begin(_cl, new Viewport(0, 0, DesignResolution.Width, DesignResolution.Height, 0, 0));
            base.Update(deltaMilliseconds);
            _canvas.End();

            _cl.End();

            _gd.SubmitCommands(_cl);
            _gd.WaitForIdle();
        }

        public void Present()
        {
            _gd.SwapBuffers();
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority).ThenBy(x => x.CreationTime);
        }

        private IEnumerable<Entity> SortByPriority(IEnumerable<Entity> entities)
        {
            return entities
                .OrderByDescending(x => x.GetComponent<Visual>().Priority)
                .ThenByDescending(x => x.CreationTime);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visual = entity.GetComponent<Visual>();
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
            visual.Render(RC);
        }

        public void Dispose()
        {
            _canvas.Dispose();
            _effectLibrary.Dispose();
            _projectionMatrix2D.Dispose();
            _camera.Dispose();
            _cl.Dispose();
        }
    }
}
