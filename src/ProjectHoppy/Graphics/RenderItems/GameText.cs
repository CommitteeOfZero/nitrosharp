using HoppyFramework.Graphics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace ProjectHoppy.Graphics.RenderItems
{
    public class GameText : Visual
    {
        private static CustomTextRenderer s_textRenderer;
        private static TextFormat s_textFormat;
        private static SolidColorBrush s_transparentTextBrush;
        private static SolidColorBrush s_currentGlyphBrush;

        private TextLayout _layout;

        public string Text { get; set; }
        public int CurrentGlyphIndex { get; set; }
        public int PrevGlyphIndex { get; set; }
        public float CurrentGlyphOpacity { get; set; }

        public override void Render(DXRenderContext renderContext)
        {
            CreateStaticResources(renderContext);

            if (_layout == null)
            {
                _layout = new TextLayout(renderContext.DWriteFactory, Text, s_textFormat, Width, Height);
            }

            s_transparentTextBrush.Color = Color;
            s_transparentTextBrush.Opacity = 0;
            s_currentGlyphBrush.Color = Color;
            s_currentGlyphBrush.Opacity = CurrentGlyphOpacity;
            renderContext.ColorBrush.Color = Color;
            renderContext.ColorBrush.Opacity = Opacity;

            //if (textComponent.Animated)
            {
                if (CurrentGlyphIndex != PrevGlyphIndex)
                {
                    _layout.SetDrawingEffect(s_currentGlyphBrush, new TextRange(CurrentGlyphIndex, 1));
                    _layout.SetDrawingEffect(renderContext.ColorBrush, new TextRange(PrevGlyphIndex, 1));
                    PrevGlyphIndex = CurrentGlyphIndex;
                }
            }

            var origin = new RawVector2(X, Y);
            _layout.Draw(s_textRenderer, X, Y);
        }

        private static void CreateStaticResources(DXRenderContext renderContext)
        {
            if (s_textRenderer == null)
            {
                s_transparentTextBrush = new SolidColorBrush(renderContext.DeviceContext, SharpDX.Color.Transparent);
                s_currentGlyphBrush = new SolidColorBrush(renderContext.DeviceContext, SharpDX.Color.Transparent);

                s_textFormat = new TextFormat(renderContext.DWriteFactory, "Noto Sans CJK JP", 20);
                s_textRenderer = new CustomTextRenderer(renderContext.DeviceContext, s_transparentTextBrush, false);
            }
        }
    }
}
