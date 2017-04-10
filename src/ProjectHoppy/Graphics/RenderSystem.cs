using System.Collections.Generic;
using System.Linq;
using HoppyFramework;
using HoppyFramework.Graphics;
using HoppyFramework.Content;
using ProjectHoppy.Graphics.RenderItems;
using System;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem, IDisposable
    {
        public RenderSystem(DXRenderContext renderContext, ContentManager contentManager)
            : base(typeof(Visual))
        {
            RenderContext = renderContext;
            Content = contentManager;

            EntityAdded += OnEntityAdded;
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var screencap = e.GetComponent<ScreenCap>();
            if (screencap != null)
            {
                screencap.Take(this);
            }
        }

        public DXRenderContext RenderContext { get; }
        public ContentManager Content { get; }
        public SharedResources SharedResources { get; private set; }

        public override void Update(float deltaMilliseconds)
        {
            using (RenderContext.NewDrawingSession(RgbaValueF.Black))
            {
                base.Update(deltaMilliseconds);
            }
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority).ThenBy(x => x.CreationTime);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var canvas = RenderContext.DeviceContext;
            var renderItem = entity.GetComponent<RenderItem>();
            if (renderItem.IsEnabled)
            {
                var originalTransform = canvas.Transform;

                var scale = renderItem.Scale;

                float centerX = renderItem.Width / 2.0f;
                float centerY = renderItem.Height / 2.0f;
                var scaleOrigin = new SharpDX.Vector2(centerX, centerY);

                canvas.Transform *= SharpDX.Matrix3x2.Scaling(scale.X, scale.Y, scaleOrigin);
                canvas.Transform *= SharpDX.Matrix3x2.Translation(renderItem.Position.X, renderItem.Position.Y);

                renderItem.Render(this);
                canvas.Transform = originalTransform;
            }
        }

        public void LoadSharedResources()
        {
            SharedResources = new SharedResources(RenderContext);
        }

        public void Dispose()
        {
            SharedResources.Dispose();
        }
    }
}
