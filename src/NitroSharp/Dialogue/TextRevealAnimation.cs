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
        private const float GlyphTime = 60;

        private TextLayout _textLayout;
        private uint _idxCurrentGlyph;

        public uint CurrentGlyphIndex => _idxCurrentGlyph;
        public bool IsAllTextVisible { get; private set; }

        public override void OnAttached()
        {
            var textLayout = Entity.GetComponent<TextLayout>();
            if (textLayout == null)
            {
                throw new InvalidOperationException("This component can't be attached to an entity that doesn't have a TextLayout component.");
            }

            Duration = CalculateDuration(textLayout);
            _textLayout = textLayout;
        }

        private static TimeSpan CalculateDuration(TextLayout textLayout)
        {
            return TimeSpan.FromMilliseconds(GlyphTime * textLayout.GlyphCount);
        }

        public override void Advance(float deltaMilliseconds)
        {
            float prevElapsed = Elapsed;
            base.Advance(deltaMilliseconds);

            uint idxCurrentGlyph = _idxCurrentGlyph;
            // First, we need to catch up (i.e. fully reveal the glyphs before
            // the one we're supposed to be animating at this time).
            uint nbCharsToReveal = (uint)(Elapsed / GlyphTime) - idxCurrentGlyph;
            nbCharsToReveal = Math.Min(nbCharsToReveal, _textLayout.GlyphCount - idxCurrentGlyph);
            RevealSpan(idxCurrentGlyph, nbCharsToReveal);
            idxCurrentGlyph += nbCharsToReveal;

            if (idxCurrentGlyph < _textLayout.GlyphCount)
            {
                float currentGlyphOpacity = (Elapsed % GlyphTime) / GlyphTime;
                ref var glyph = ref _textLayout.MutateGlyph(idxCurrentGlyph);
                SetOpacity(ref glyph, currentGlyphOpacity);
                _idxCurrentGlyph = idxCurrentGlyph;
            }

            PostAdvance();
            if (Progress == 1.0f)
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
            ref var glyph = ref _textLayout.MutateGlyph(_idxCurrentGlyph);
            Reveal(ref glyph);
            _idxCurrentGlyph = Math.Min(_idxCurrentGlyph + 1, _textLayout.GlyphCount - 1);
            IsAllTextVisible = _idxCurrentGlyph >= _textLayout.GlyphCount - 1;
        }
    }
}
