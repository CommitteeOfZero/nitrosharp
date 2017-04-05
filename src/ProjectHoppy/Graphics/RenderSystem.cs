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
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            entity.GetComponent<RenderItem>().Render(this);
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
