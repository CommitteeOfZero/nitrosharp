using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Graphics.Text;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem
    {
        private Dictionary<TextComponent, TextLayout> _textLayouts;
        private TextFormat _textFormat;

        private ColorBrush _defaultTextBrush;
        private ColorBrush _blackBrush;
        private ColorBrush _currentGlyphBrush;

        private void CreateTextResources()
        {
            _textLayouts = new Dictionary<TextComponent, TextLayout>();
            _textFormat = new TextFormat
            {
                FontFamily = "Noto Sans CJK JP",
                FontSize = 20,
                FontWeight = FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _defaultTextBrush = _rc.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
            _blackBrush = _rc.ResourceFactory.CreateColorBrush(RgbaValueF.White, 1.0f);
            _currentGlyphBrush = _rc.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
        }

        public override void OnEnityAdded(Entity e)
        {
			if (e.HasComponent<TextComponent>())
            {
                var visual = e.GetComponent<VisualComponent>();
                var txt = e.GetComponent<TextComponent>();
                var layout = _rc.ResourceFactory.CreateTextLayout(txt.Text, _textFormat, visual.Width, visual.Height);
                _textLayouts[txt] = layout;
            }
        }

        public override void OnEntityRemoved(Entity e)
        {
            base.OnEntityRemoved(e);
        }

        private void DrawText(VisualComponent visualComponent, TextComponent textComponent)
        {
            var layout = _textLayouts[textComponent];
            //_currentGlyphBrush.Opacity = textComponent.CurrentGlyphOpacity;
            //layout.SetGlyphBrush(textComponent.CurrentGlyphIndex, _currentGlyphBrush);

            //if (textComponent.ResetBrushFlag && textComponent.CurrentGlyphIndex > 0)
            //{
            //    textComponent.ResetBrushFlag = false;
            //    layout.SetGlyphBrush(textComponent.CurrentGlyphIndex - 1, _blackBrush);
            //}

            _drawingSession.DrawTextLayout(layout, new Vector2(visualComponent.X, visualComponent.Y), _blackBrush.Color);
        }
    }
}
