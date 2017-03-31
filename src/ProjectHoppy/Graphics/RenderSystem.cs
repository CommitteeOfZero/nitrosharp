using System.Collections.Generic;
using System.Linq;
using ProjectHoppy.Text;
using HoppyFramework;
using HoppyFramework.Graphics;
using HoppyFramework.Content;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem
    {
        private DXRenderContext _rc;
        private readonly ContentManager _content;
        private DXDrawingSession _drawingSession;

        private SolidColorBrush _colorBrush;

        public RenderSystem(DXRenderContext renderContext, ContentManager contentManager)
            : base(typeof(VisualComponent))
        {
            _rc = renderContext;
            _content = contentManager;

            EntityAdded += OnTextAdded;

            _colorBrush = new SolidColorBrush(_rc.DeviceContext, RgbaValueF.White);
            CreateTextResources();
        }

        public override void Update(float deltaMilliseconds)
        {
            using (_drawingSession = _rc.NewDrawingSession(RgbaValueF.Black))
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
            }
        }

        private void DrawRectangle(VisualComponent visual)
        {
            _colorBrush.Color = visual.Color;
            _colorBrush.Opacity = visual.Opacity;

            var dest = new SharpDX.RectangleF(visual.X, visual.Y, visual.Width, visual.Height);
            _rc.DeviceContext.FillRectangle(dest, _colorBrush);
        }

        private void DrawTexture(VisualComponent visual, TextureComponent textureComponent)
        {
            if (_content.TryGetAsset<TextureAsset>(textureComponent.AssetRef, out var texture))
            {
                var dest = new RawRectangleF(visual.X, visual.Y, texture.Width, texture.Height);
                _rc.DeviceContext.DrawBitmap(texture, dest, visual.Opacity, BitmapInterpolationMode.Linear);
            }
        }
    }
}
