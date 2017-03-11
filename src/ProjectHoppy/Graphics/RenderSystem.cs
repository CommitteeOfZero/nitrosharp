using System.Collections.Generic;
using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using System.Linq;
using SciAdvNet.MediaLayer.Graphics.Text;
using System.Numerics;

namespace ProjectHoppy.Graphics
{
    public class RenderSystem : EntityProcessingSystem
    {
        private RenderContext _rc;
        private DrawingSession _drawingSession;

        public RenderSystem(RenderContext renderContext)
            : base(typeof(VisualComponent))
        {
            _rc = renderContext;
        }

        public override void Update(float deltaMilliseconds)
        {
            using (_drawingSession = _rc.NewSession(Color.White))
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
                    DrawText(e);
                    break;
            }
        }

        private void DrawShape(VisualComponent visualComponent, ShapeComponent shapeComponent)
        {
            _drawingSession.FillRectangle(visualComponent.X, visualComponent.Y, visualComponent.Width, visualComponent.Height, shapeComponent.FillColor);
        }

        private Dictionary<Entity, TextLayout> _layouts = new Dictionary<Entity, TextLayout>();

        private void DrawText(Entity e)
        {
            var format = new TextFormat
            {
                FontFamily = "Arial",
                FontSize = 28,

            };

            var text = e.GetComponent<TextComponent>();
            if (!_layouts.TryGetValue(e, out var layout))
            {
                

                layout = _rc.ResourceFactory.CreateTextLayout(text.Text, format, 200, 200);
                _layouts[e] = layout;
            }

            
            _drawingSession.DrawTextLayout(layout, Vector2.Zero, new Color(0, 0, 0, 100));
            layout.SetGlyphColor(1, text.CurrentGlyphColor);
        }
    }
}
