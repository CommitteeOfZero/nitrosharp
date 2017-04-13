using MoeGame.Framework.Graphics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace CommitteeOfZero.Nitro.Graphics.RenderItems
{
    public class GameTextVisual : Visual
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

        public GameTextVisual()
        {

        }

        public void Reset()
        {
            _layout?.Dispose();
            _layout = null;
            PrevGlyphIndex = 0;
            CurrentGlyphIndex = 0;
            CurrentGlyphOpacity = 0.0f;
        }

        public override void Render(RenderSystem renderSystem)
        {
            var context = renderSystem.RenderContext;
            CreateStaticResources(context);

            if (_layout == null)
            {
                _layout = new TextLayout(context.DWriteFactory, Text, s_textFormat, Width, Height);
            }

            s_transparentTextBrush.Color = Color;
            s_transparentTextBrush.Opacity = 0;
            s_currentGlyphBrush.Color = Color;
            s_currentGlyphBrush.Opacity = CurrentGlyphOpacity;
            context.ColorBrush.Color = Color;
            context.ColorBrush.Opacity = Opacity;

            //if (textComponent.Animated)
            {
                if (CurrentGlyphIndex != PrevGlyphIndex)
                {
                    _layout.SetDrawingEffect(s_currentGlyphBrush, new TextRange(CurrentGlyphIndex, 1));
                    _layout.SetDrawingEffect(context.ColorBrush, new TextRange(PrevGlyphIndex, 1));
                    PrevGlyphIndex = CurrentGlyphIndex;
                }
            }

            _layout.Draw(s_textRenderer, 0, 0);
        }

        private static void CreateStaticResources(DxRenderContext renderContext)
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
