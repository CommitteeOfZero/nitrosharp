using CommitteeOfZero.NitroSharp.Foundation.Animation;
using System;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public sealed class TextSkipAnimation : AnimationBase
    {
        private TextVisual _textVisual;

        public TextSkipAnimation() : base(TimeSpan.FromMilliseconds(200))
        {
        }

        public override void OnAttached()
        {
            var textVisual = Entity.GetComponent<TextVisual>();
            if (textVisual == null)
            {
                throw new InvalidOperationException("TextSkipAnimation component can't be attached to an entity that doesn't have a TextVisual component.");
            }

            _textVisual = textVisual;
            int skipStartIndex = _textVisual.AnimatedRegion.RangeStart + 1;
            _textVisual.AnimatedRegion = new TextRange(skipStartIndex, _textVisual.Text.Length - skipStartIndex);
        }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);
            _textVisual.AnimatedOpacity = CalculateFactor(Progress, TimingFunction);

            if (LastFrame)
            {
                RaiseCompleted();
            }
        }
    }
}
