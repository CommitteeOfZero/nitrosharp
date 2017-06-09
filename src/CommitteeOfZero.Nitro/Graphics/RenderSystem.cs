using System.Collections.Generic;
using System.Linq;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Graphics;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private INitroRenderer _renderer;

        public RenderSystem(DxRenderContext renderContext)
        {
            RenderContext = renderContext;
        }

        public DxRenderContext RenderContext { get; }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var screencap = entity.GetComponent<Screenshot>();
            screencap?.Take(_renderer);
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            var visual = entity.GetComponent<Visual>();
            visual.Free(_renderer);
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
                transform *= Matrix3x2.CreateScale(RenderContext.BackBufferSize.Width / 1280.0f, RenderContext.BackBufferSize.Height / 720.0f);
                //transform.Translation = new Vector2((float)Math.Floor(transform.Translation.X), (float)Math.Floor(transform.Translation.Y));

                _renderer.SetTransform(transform);
                visual.Render(_renderer);
            }
        }

        public void LoadCommonResources()
        {
            _renderer = new DxNitroRenderer(RenderContext, new System.Drawing.Size(1280, 720));
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }
}
