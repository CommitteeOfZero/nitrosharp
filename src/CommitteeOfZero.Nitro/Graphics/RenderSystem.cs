using System.Collections.Generic;
using System.Linq;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Graphics;
using CommitteeOfZero.Nitro.Foundation.Content;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private ICanvas _canvas;

        public RenderSystem(DxRenderContext renderContext, ContentManager contentManager)
        {
            RenderContext = renderContext;
            Content = contentManager;
        }

        public ICanvas Canvas => _canvas;
        public DxRenderContext RenderContext { get; }
        public ContentManager Content { get; }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var screencap = entity.GetComponent<ScreenshotVisual>();
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
                //var transform = Matrix3x2.Identity;

                //float centerX = 0;//visual.Width / 2.0f;
                //float centerY = 0;//visual.Height / 2.0f;
                //var scaleOrigin = new Vector2(centerX, centerY);

                //transform *= Matrix3x2.CreateScale(entity.Transform.LocalScale, scaleOrigin);
                //transform *= Matrix3x2.CreateTranslation(entity.Transform.LocalPosition);
                //if (entity.Transform.Parent != null)
                //{
                //    var parent = entity.Transform.Parent;
                //    transform *= Matrix3x2.CreateTranslation(parent.Position);
                //}

                var transform = entity.Transform.GetWorldMatrix(new System.Drawing.SizeF(1280, 720));
                _canvas.SetTransform(transform);
                visual.Render(_canvas);
            }
        }

        public void LoadCommonResources()
        {
            _canvas = new DxCanvas(RenderContext, Content);
        }

        public void Dispose()
        {
            _canvas.Dispose();
        }
    }
}
