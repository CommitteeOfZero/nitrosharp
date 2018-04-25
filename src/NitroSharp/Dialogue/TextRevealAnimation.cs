using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Graphics.Objects;
using NitroSharp.Primitives;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class TextRevealAnimation : AnimationBase
    {
        private const float GlyphTime = 50;

        private readonly TextLayout _textLayout;
        private readonly uint _startIndex;
        private uint _offset;

        public TextRevealAnimation(TextLayout textLayout, uint startPosition)
            : base(CalculateDuration(textLayout, startPosition))
        {
            _textLayout = textLayout;
            _startIndex = startPosition;
        }

        public uint Position => _startIndex + _offset;
        public bool IsAllTextVisible { get; private set; }

        private static TimeSpan CalculateDuration(TextLayout textLayout, uint start)
        {
            return TimeSpan.FromMilliseconds(GlyphTime * (textLayout.GlyphCount - start));
        }

        public override void Advance(float deltaMilliseconds)
        {
            float prevElapsed = Elapsed;
            base.Advance(deltaMilliseconds);

            // First, we need to catch up (i.e. fully reveal the glyphs before
            // the one we're supposed to be animating at this time).
            uint nbCharsToReveal = (uint)(Elapsed / GlyphTime) - _offset;
            nbCharsToReveal = Math.Min(nbCharsToReveal, _textLayout.GlyphCount - Position);
            RevealSpan(Position, nbCharsToReveal);
            _offset += nbCharsToReveal;

            if (Position < _textLayout.GlyphCount)
            {
                float currentGlyphOpacity = (Elapsed % GlyphTime) / GlyphTime;
                ref var glyph = ref _textLayout.MutateGlyph(Position);
                SetOpacity(ref glyph, currentGlyphOpacity);
            }

            PostAdvance();
            if (HasCompleted)
            {
                IsAllTextVisible = true;
            }
        }

        private void RevealSpan(uint start, uint length)
        {
            var span = _textLayout.MutateSpan(start, length);
            for (int i = 0; i < span.Length; i++)
            {
                Reveal(ref span[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reveal(ref LayoutGlyph glyph) => SetOpacity(ref glyph, 1.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOpacity(ref LayoutGlyph glyph, float opacity)
        {
            glyph.Color = glyph.Color.SetAlpha(opacity);
        }

        public void Stop()
        {
            IsEnabled = false;
            ref var glyph = ref _textLayout.MutateGlyph(Position);
            Reveal(ref glyph);
            IsAllTextVisible = Position == (_textLayout.GlyphCount - 1);
        }
    }
}
