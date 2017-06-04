using CommitteeOfZero.Nitro.Foundation.Animation;
using System;
using System.Linq;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class SmoothTextAnimation : AnimationBase
    {
        private const float GlyphTime = 80.0f;

        private TextVisual _textVisual;
        private float _elapsed2;

        public override void OnAttached()
        {
            var textVisual = Entity.GetComponent<TextVisual>();
            if (textVisual == null)
            {
                throw new InvalidOperationException("SmoothTextAnimation component can't be attached to an entity that doesn't have a TextVisual component.");
            }

            Duration = CalculateDuration(textVisual.Text);
            _textVisual = textVisual;
        }

        private static TimeSpan CalculateDuration(string text)
        {
            int glyphCount = text.Count(c => c != ' ');
            return TimeSpan.FromMilliseconds(GlyphTime * glyphCount);
        }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);
            _elapsed2 += deltaMilliseconds;

            int idxCurrentGlyph = _textVisual.AnimatedRegion.RangeStart;
            if (_elapsed2 >= GlyphTime)
            {
                idxCurrentGlyph += (int)(_elapsed2 / GlyphTime);
                while (idxCurrentGlyph < _textVisual.Text.Length &&_textVisual.Text[idxCurrentGlyph] == ' ')
                {
                    idxCurrentGlyph++;
                }

                idxCurrentGlyph = Math.Min(idxCurrentGlyph, _textVisual.Text.Length - 1);
                _elapsed2 = _elapsed2 % GlyphTime;
            }

            if (!LastFrame)
            {
                _textVisual.AnimatedOpacity = SharpDX.MathUtil.Clamp(_elapsed2 / GlyphTime, 0.0f, 1.0f);
                _textVisual.AnimatedRegion = new TextRange(idxCurrentGlyph, 1);
                _textVisual.VisibleRegion = new TextRange(0, idxCurrentGlyph);
            }
            else
            {
                _textVisual.VisibleRegion = new TextRange(0, _textVisual.Text.Length);
                _textVisual.AnimatedRegion = new TextRange(0, 0);

                RaiseCompleted();
            }
        }

        public void Stop()
        {
            IsEnabled = false;
            if (_textVisual.AnimatedOpacity < 1.0f)
            {
                _textVisual.AnimatedOpacity = 1.0f;
                _textVisual.VisibleRegion = new TextRange(0, _textVisual.VisibleRegion.Length + 1);
                _textVisual.AnimatedRegion = new TextRange(_textVisual.AnimatedRegion.RangeStart, 0);
            }
        }
    }
}
