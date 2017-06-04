using SharpDX.DirectWrite;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed partial class DxCanvas
    {
        private TextLayout _textLayout;
        private TextRenderer _customTextRenderer;
        private TextFormat _textFormat;

        private (TextRange range, TextDrawingContext context) _animatedRegion;
        private (TextRange range, TextDrawingContext context) _visibleRegion;

        private void CreateTextResources()
        {
            _customTextRenderer = new CustomTextRenderer(_rc.DeviceContext, _rc.ColorBrush);
            _textFormat = new TextFormat(_rc.DWriteFactory, "Segoe UI", 30);

            _visibleRegion.context = new TextDrawingContext { OpacityOverride = 1.0f };
            _animatedRegion.context = new TextDrawingContext();
        }

        public void DrawText(TextVisual text)
        {
            if (_textLayout == null || _textLayout.IsDisposed)
            {
                _textLayout = new TextLayout(_rc.DWriteFactory, text.Text, _textFormat, text.Measure().Width, text.Measure().Height);
            }

            _rc.ColorBrush.Color = text.Color;
            _rc.ColorBrush.Opacity = 0;

            if (_animatedRegion.range != text.AnimatedRegion)
            {
                var range = _animatedRegion.range = text.AnimatedRegion;
                if (range.Length > 0)
                {
                    _textLayout.SetDrawingEffect(_animatedRegion.context, new SharpDX.DirectWrite.TextRange(range.RangeStart, range.Length));
                }
            }

            if (_visibleRegion.range != text.VisibleRegion)
            {
                var range = _visibleRegion.range = text.VisibleRegion;
                if (range.Length > 0)
                {
                    _textLayout.SetDrawingEffect(_visibleRegion.context, new SharpDX.DirectWrite.TextRange(range.RangeStart, range.Length));
                }
            }

            _animatedRegion.context.OpacityOverride = text.AnimatedOpacity;
            _textLayout.Draw(_customTextRenderer, 0, 0);
        }

        public void Free(TextVisual textVisual)
        {
            _textLayout.Dispose();
        }
    }
}
