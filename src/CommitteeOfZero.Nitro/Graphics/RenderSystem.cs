using System.Collections.Generic;
using System.Linq;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Graphics;
using System;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private ICanvas _canvas;

        public RenderSystem(DxRenderContext renderContext)
        {
            RenderContext = renderContext;
        }

        public ICanvas Canvas => _canvas;
        public DxRenderContext RenderContext { get; }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var screencap = entity.GetComponent<Screenshot>();
            screencap?.Take(_canvas);
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            var visual = entity.GetComponent<Visual>();
            visual.Free(_canvas);
        }

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
            var visual = entity.GetComponent<Visual>();
            if (visual.IsEnabled)
            {
                var transform = entity.Transform.GetWorldMatrix(new System.Drawing.SizeF(1280, 720));
                _canvas.SetTransform(transform);
                visual.Render(_canvas);
            }
        }

        public void LoadCommonResources()
        {
            _canvas = new DxCanvas(RenderContext);
        }

        public void Dispose()
        {
            _canvas.Dispose();
        }
    }
}
