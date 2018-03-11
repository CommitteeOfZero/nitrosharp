using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Veldrid;
using NitroSharp.Logic;
using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private readonly NewNitroConfiguration _config;
        private readonly CommandList cl;
        private readonly GraphicsDevice _gd;
        private readonly ResourceFactory _factory;
        private readonly Canvas _canvas;

        public RenderSystem(GraphicsDevice graphicsDevice, NewNitroConfiguration configuration)
        {
            _gd = graphicsDevice;
            _config = configuration;

            
            _factory = _gd.ResourceFactory;
            cl = _factory.CreateCommandList();
            _canvas = new Canvas(graphicsDevice);
        }

        private SizeF DesignResolution => new SizeF(_config.WindowWidth, _config.WindowHeight);

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual2D));
        }

        public override void Update(float deltaMilliseconds)
        {
            cl.Begin();

            cl.SetFramebuffer(_gd.SwapchainFramebuffer);
            cl.SetFullViewports();
            cl.ClearColorTarget(0, RgbaFloat.Black);

            _canvas.Begin(cl, new Viewport(0, 0, DesignResolution.Width, DesignResolution.Height, 0, 0));
            base.Update(deltaMilliseconds);
            _canvas.End();

            cl.End();

            _gd.SubmitCommands(cl);
            _gd.WaitForIdle();
        }

        public void Present()
        {
            _gd.SwapBuffers();
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => ((Visual2D)x.GetComponent<Visual2D>()).Priority).ThenBy(x => x.CreationTime);
        }

        private IEnumerable<Entity> SortByPriority(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities.OrderByDescending(x => x.GetComponent<Visual2D>().Priority).ThenByDescending(x => x.CreationTime))
            {
                yield return entity;
            }
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visual = entity.GetComponent<Visual2D>();
            if (visual.IsEnabled)
            {
                RenderItem(visual, Vector2.One);
            }
        }

        private void RenderItem(Visual2D visual, Vector2 scale)
        {
            var transform = visual.Entity.Transform.GetWorldMatrix();
            _canvas.SetTransform(transform);
            visual.Render(_canvas);
        }

        public void Dispose()
        {
            _canvas.Dispose();
        }
    }
}
