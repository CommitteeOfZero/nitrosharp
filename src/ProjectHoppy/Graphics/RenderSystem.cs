using System.Collections.Generic;
using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using System.Linq;
using SciAdvNet.MediaLayer.Graphics.Text;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem
    {
        private RenderContext _rc;
        private DrawingSession _drawingSession;

        public RenderSystem(RenderContext renderContext)
            : base(typeof(VisualComponent))
        {
            _rc = renderContext;

            _textLayouts = new Dictionary<Entity, TextLayout>();
            _textFormat = new TextFormat
            {
                FontFamily = "Noto Sans CJK JP",
                FontSize = 20,
                FontWeight = FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _defaultTextBrush = renderContext.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
            _blackBrush = renderContext.ResourceFactory.CreateColorBrush(RgbaValueF.White, 1.0f);
            _currentGlyphBrush = renderContext.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
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
            switch (entity)
            {
                case Entity e when entity.HasComponent<ShapeComponent>():
                    DrawShape(visualComponent, e.GetComponent<ShapeComponent>());
                    break;

                case Entity e when entity.HasComponent<TextComponent>():
                    DrawText(e, visualComponent, e.GetComponent<TextComponent>());
                    break;
            }
        }

        private void DrawShape(VisualComponent visualComponent, ShapeComponent shapeComponent)
        {
            _drawingSession.FillRectangle(visualComponent.X, visualComponent.Y, visualComponent.Width, visualComponent.Height, shapeComponent.FillColor);
        }
    }
}
