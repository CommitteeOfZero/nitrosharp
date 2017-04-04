using System.Collections.Generic;
using System.Linq;
using ProjectHoppy.Text;
using HoppyFramework;
using HoppyFramework.Graphics;
using HoppyFramework.Content;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using ProjectHoppy.Graphics.Effects;
using System.Diagnostics;
using System;
using SharpDX;
using ProjectHoppy.Graphics.RenderItems;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem : EntityProcessingSystem
    {
        private DXRenderContext _rc;
        private readonly ContentManager _content;
        private DXDrawingSession _drawingSession;

        private SolidColorBrush _colorBrush;

        private Bitmap1 _screenshot;

        public RenderSystem(DXRenderContext renderContext, ContentManager contentManager)
            : base(typeof(VisualComponent), typeof(RenderItem))
        {
            _rc = renderContext;
            _content = contentManager;

            //EntityAdded += OnTextAdded;
            //EntityAdded += OnScreenshotRequested;

            _colorBrush = new SolidColorBrush(_rc.DeviceContext, RgbaValueF.White);
            CreateTextResources();
        }

        private void OnScreenshotRequested(object sender, Entity e)
        {
            //if (e.GetComponent<VisualComponent>().Kind == VisualKind.Screenshot)
            //{
            //    _screenshot = new Bitmap1(_rc.DeviceContext, new Size2(800, 600), new BitmapProperties1(_rc.DeviceContext.PixelFormat, 96, 96));
            //    _screenshot.CopyFromRenderTarget(_rc.DeviceContext);
            //}
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
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visualComponent = entity.GetComponent<VisualComponent>();
            if (visualComponent != null)
            {
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

                    case VisualKind.DissolveTransition:
                        DrawTransition(visualComponent, entity.GetComponent<DissolveTransition>());
                        break;

                    case VisualKind.Screenshot:
                        DrawScreenshot(visualComponent);
                        break;
                }
            }
            else
            {
                entity.GetComponent<RenderItem>().Render(_rc);
            }
        }

        private void DrawScreenshot(VisualComponent visual)
        {
            var dest = new RawRectangleF(visual.X, visual.Y, _screenshot.Size.Width, _screenshot.Size.Height);
            _rc.DeviceContext.DrawBitmap(_screenshot, dest, visual.Opacity, BitmapInterpolationMode.Linear);
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

        private Effect<DissolveEffect> _effect = null;

        private void DrawTransition(VisualComponent visual, DissolveTransition transition)
        {
            if (_content.TryGetAsset<TextureAsset>(transition.Texture, out var texture)
                && _content.TryGetAsset<TextureAsset>(transition.AlphaMask, out var alphaMask))
            {
                if (_effect == null)
                {
                    _rc.D2DFactory.RegisterEffect<DissolveEffect>();

                    _effect = new Effect<DissolveEffect>(_rc.DeviceContext);
                    _effect.SetInput(0, texture, true);
                    _effect.SetInput(1, alphaMask, true);

                }
                
                _effect.SetValue(0, transition.Opacity);
                _rc.DeviceContext.DrawImage(_effect);
            }
        }
    }
}
