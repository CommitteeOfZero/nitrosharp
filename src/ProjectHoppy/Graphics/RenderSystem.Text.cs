using ProjectHoppy.Framework;
using ProjectHoppy.Text;
using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Graphics.Text;
using System.Collections.Generic;
using System.Numerics;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem
    {
        private Dictionary<TextComponent, TextLayout> _textLayouts;
        private TextFormat _textFormat;

        private ColorBrush _transparentTextBrush;
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

            _transparentTextBrush = _rc.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
            _currentGlyphBrush = _rc.ResourceFactory.CreateColorBrush(RgbaValueF.White, 0.0f);
        }
        
        private void OnTextAdded(object sender, Entity e)
        {
			if (e.HasComponent<TextComponent>())
            {
                var visual = e.GetComponent<VisualComponent>();
                var txt = e.GetComponent<TextComponent>();
                var layout = _rc.ResourceFactory.CreateTextLayout(txt.Text, _textFormat, visual.Width, visual.Height);
                _textLayouts[txt] = layout;
            }
        }

        private void DrawText(VisualComponent visualComponent, TextComponent textComponent)
        {
            var layout = _textLayouts[textComponent];

            _transparentTextBrush.Color = visualComponent.Color;
            _transparentTextBrush.Opacity = 0;
            _colorBrush.Color = visualComponent.Color;
            _colorBrush.Opacity = visualComponent.Opacity;
            _currentGlyphBrush.Color = visualComponent.Color;
            _currentGlyphBrush.Opacity = textComponent.CurrentGlyphOpacity;

            if (textComponent.Animated)
            {
                if (textComponent.CurrentGlyphIndex != textComponent.PrevGlyphIndex)
                {
                    layout.SetGlyphBrush(textComponent.CurrentGlyphIndex, _currentGlyphBrush);
                    layout.SetGlyphBrush(textComponent.PrevGlyphIndex, _colorBrush);
                    textComponent.PrevGlyphIndex = textComponent.CurrentGlyphIndex;
                }
            }

            _drawingSession.DrawTextLayout(layout, new Vector2(visualComponent.X, visualComponent.Y), new RgbaValueF(1, 1, 1, 0));
        }
    }
}
