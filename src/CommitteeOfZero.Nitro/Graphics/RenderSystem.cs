using System.Collections.Generic;
using System.Linq;
using MoeGame.Framework;
using MoeGame.Framework.Graphics;
using MoeGame.Framework.Content;
using CommitteeOfZero.Nitro.Graphics.Visuals;
using System;

namespace CommitteeOfZero.Nitro.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem, IDisposable
    {
        public RenderSystem(DxRenderContext renderContext, ContentManager contentManager)
        {
            RenderContext = renderContext;
            Content = contentManager;

            EntityAdded += OnEntityAdded;
        }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var screencap = e.GetComponent<ScreenCap>();
            screencap?.Take(this);
        }

        public DxRenderContext RenderContext { get; }
        public ContentManager Content { get; }
        public CommonResources CommonResources { get; private set; }

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
            var visual = entity.GetComponent<Visual>();
            if (visual.IsEnabled)
            {
                var originalTransform = canvas.Transform;
                var scale = visual.Scale;

                float centerX = visual.Width / 2.0f;
                float centerY = visual.Height / 2.0f;
                var scaleOrigin = new SharpDX.Vector2(centerX, centerY);

                canvas.Transform *= SharpDX.Matrix3x2.Scaling(scale.X, scale.Y, scaleOrigin);
                if (visual.ParentVisual != null)
                {
                    var parent = visual.ParentVisual;
                    canvas.Transform *= SharpDX.Matrix3x2.Translation(parent.Position.X + visual.Position.X, parent.Position.Y + visual.Position.Y);
                }
                else
                {
                    canvas.Transform *= SharpDX.Matrix3x2.Translation(visual.Position.X, visual.Position.Y);
                }

                visual.Render(this);
                canvas.Transform = originalTransform;
            }
        }

        public void LoadCommonResources()
        {
            CommonResources = new CommonResources(RenderContext);
        }

        public void Dispose()
        {
            CommonResources.Dispose();
        }
    }
}
