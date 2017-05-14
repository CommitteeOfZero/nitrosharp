using System.Collections.Generic;
using System.Linq;
using MoeGame.Framework;
using MoeGame.Framework.Graphics;
using MoeGame.Framework.Content;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private ICanvas _canvas;

        public RenderSystem(DxRenderContext renderContext, ContentManager contentManager)
        {
            RenderContext = renderContext;
            Content = contentManager;
        }

        public DxRenderContext RenderContext { get; }
        public ContentManager Content { get; }
        public CommonResources CommonResources { get; private set; }

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
                var transform = Matrix3x2.Identity;

                float centerX = visual.Width / 2.0f;
                float centerY = visual.Height / 2.0f;
                var scaleOrigin = new Vector2(centerX, centerY);

                transform *= Matrix3x2.CreateScale(visual.Scale, scaleOrigin);
                transform *= Matrix3x2.CreateTranslation(visual.Position);
                if (visual.ParentVisual != null)
                {
                    var parent = visual.ParentVisual;
                    transform *= Matrix3x2.CreateTranslation(parent.Position);
                }  

                _canvas.SetTransform(transform);
                visual.Render(_canvas);
            }
        }

        public void LoadCommonResources()
        {
            CommonResources = new CommonResources(RenderContext);
            _canvas = new DxCanvas(RenderContext, Content);
        }

        public void Dispose()
        {
        }
    }
}
