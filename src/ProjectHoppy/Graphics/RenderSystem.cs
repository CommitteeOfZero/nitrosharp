using System.Collections.Generic;
using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using System.Linq;
using ProjectHoppy.Content;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem
    {
        private RenderContext _rc;
        private readonly ConcurrentContentManager _content;
        private DrawingSession _drawingSession;

        public RenderSystem(RenderContext renderContext, ConcurrentContentManager contentManager)
            : base(typeof(VisualComponent))
        {
            _rc = renderContext;
            _content = contentManager;

            CreateTextResources();
        }

        public override void Update(float deltaMilliseconds)
        {
            using (_drawingSession = _rc.NewSession(RgbaValueF.Black))
            {
                base.Update(deltaMilliseconds);
            }
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => x.GetComponent<VisualComponent>().LayerDepth);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visualComponent = entity.GetComponent<VisualComponent>();
            switch (visualComponent.Kind)
            {
                case VisualKind.Rectangle:
                    DrawRectangle(visualComponent);
                    break;

                case VisualKind.Texture:
                    DrawTexture(visualComponent, entity.GetComponent<AssetComponent>());
                    break;

                case VisualKind.Text:
                    DrawText(visualComponent, entity.GetComponent<TextComponent>());
                    break;
            }
        }

        private void DrawRectangle(VisualComponent visual)
        {
            _drawingSession.FillRectangle(visual.X, visual.Y, visual.Width, visual.Height, visual.Color);
        }

        private void DrawTexture(VisualComponent visual, AssetComponent asset)
        {
            if (asset != null && _content.IsLoaded(asset.AssetPath))
            {
                var texture = _content.Get<Texture2D>(asset.AssetPath);
                _drawingSession.DrawTexture(texture, visual.X, visual.Y, visual.Opacity);
            }
        }
    }
}
