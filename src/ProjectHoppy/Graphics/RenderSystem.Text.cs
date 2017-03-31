using HoppyFramework;
using HoppyFramework.Graphics;
using ProjectHoppy.Text;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Numerics;

namespace ProjectHoppy.Graphics
{
    public partial class RenderSystem
    {
        private CustomBrushTextRenderer _textRenderer;
        private Dictionary<TextComponent, TextLayout> _textLayouts;
        private TextFormat _textFormat;

        private SolidColorBrush _transparentTextBrush;
        private SolidColorBrush _currentGlyphBrush;

        private void CreateTextResources()
        {
            _textLayouts = new Dictionary<TextComponent, TextLayout>();
            _textFormat = new TextFormat(_rc.DWriteFactory, "Noto Sans CJK JP", 20);

            _transparentTextBrush = new SolidColorBrush(_rc.DeviceContext, RgbaValueF.White);
            _transparentTextBrush.Opacity = 0.0f;

            _currentGlyphBrush = new SolidColorBrush(_rc.DeviceContext, RgbaValueF.White);
            _currentGlyphBrush.Opacity = 0.0f;

            _textRenderer = new CustomBrushTextRenderer(_rc.DeviceContext, _transparentTextBrush, false);
        }
        
        private void OnTextAdded(object sender, Entity e)
        {
			if (e.HasComponent<TextComponent>())
            {
                var visual = e.GetComponent<VisualComponent>();
                var txt = e.GetComponent<TextComponent>();
                var layout = new TextLayout(_rc.DWriteFactory, txt.Text, _textFormat, visual.Width, visual.Height);
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
                    layout.SetDrawingEffect(_currentGlyphBrush, new TextRange(textComponent.CurrentGlyphIndex, 1));
                    layout.SetDrawingEffect(_colorBrush, new TextRange(textComponent.PrevGlyphIndex, 1));
                    textComponent.PrevGlyphIndex = textComponent.CurrentGlyphIndex;
                }
            }

            var origin = new RawVector2(visualComponent.X, visualComponent.Y);
            layout.Draw(_textRenderer, visualComponent.X, visualComponent.Y);
        }
    }
}
