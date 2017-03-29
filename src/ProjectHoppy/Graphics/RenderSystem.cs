using System.Collections.Generic;
using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using System.Linq;
using ProjectHoppy.Framework.Content;
using ProjectHoppy.Text;
using ProjectHoppy.Framework;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem
    {
        private RenderContext _rc;
        private readonly ContentManager _content;
        private DrawingSession _drawingSession;

        private ColorBrush _colorBrush;

        private Dictionary<Texture2D, BitmapBrush> _bitmapBrushCache;

        public RenderSystem(RenderContext renderContext, ContentManager contentManager)
            : base(typeof(VisualComponent))
        {
            _rc = renderContext;
            _content = contentManager;

            EntityAdded += OnTextAdded;

            _bitmapBrushCache = new Dictionary<Texture2D, BitmapBrush>();

            _colorBrush = renderContext.ResourceFactory.CreateColorBrush(RgbaValueF.White, 1.0f);
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
            return entities.OrderBy(x => x.GetComponent<VisualComponent>().Priority);
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
                    DrawTexture(visualComponent, entity.GetComponent<TextureComponent>());
                    break;

                case VisualKind.Text:
                    DrawText(visualComponent, entity.GetComponent<TextComponent>());
                    break;

                case VisualKind.MaskEffect:
                    DrawMaskEffect(visualComponent, entity.GetComponent<MaskEffect>());
                    break;
            }
        }

        private void DrawRectangle(VisualComponent visual)
        {
            _colorBrush.Color = visual.Color;
            _colorBrush.Opacity = visual.Opacity;

            _drawingSession.FillRectangle(visual.X, visual.Y, visual.Width, visual.Height, _colorBrush);
        }

        private void DrawTexture(VisualComponent visual, TextureComponent textureComponent)
        {
            if (_content.TryGetAsset<Texture2D>(textureComponent.AssetRef, out var texture))
            {
                _drawingSession.DrawTexture(texture, visual.X, visual.Y, visual.Opacity);
            }
        }

        private void DrawMaskEffect(VisualComponent visual, MaskEffect maskEffect)
        {
            if (_content.TryGetAsset<Texture2D>(maskEffect.TextureRef, out var texture)
                && _content.TryGetAsset<Texture2D>(maskEffect.MaskRef, out var mask))
            {
                if (!_bitmapBrushCache.TryGetValue(texture, out var bitmapBrush))
                {
                    bitmapBrush = _rc.ResourceFactory.CreateBitmapBrush(texture, visual.Opacity);
                    _bitmapBrushCache[texture] = bitmapBrush;
                }

                bitmapBrush.Opacity = visual.Opacity;
                //_drawingSession.FillOpacityMask(mask, bitmapBrush);
            }
        }
    }
}
