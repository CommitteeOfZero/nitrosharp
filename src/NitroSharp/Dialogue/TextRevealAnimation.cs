using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Primitives;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class TextRevealAnimation : AnimationBase
    {
        private const float GlyphTime = 50;

        private readonly ushort _startIndex;
        private ushort _offset;
        private World _world;

        public TextRevealAnimation(World world, Entity textEntity, ushort startPosition)
            : base(textEntity, CalculateDuration(world.TextInstances.Layouts.Mutate(textEntity), startPosition))
        {
            _world = world;
            _startIndex = startPosition;
        }

        public ushort Position => (ushort)(_startIndex + _offset);
        public bool IsAllTextVisible { get; private set; }

        private static TimeSpan CalculateDuration(TextLayout textLayout, ushort start)
        {
            return TimeSpan.FromMilliseconds(GlyphTime * (textLayout.Glyphs.Count - start));
        }

        protected override void Advance(World world, float deltaMilliseconds)
        {
            TextLayout textLayout = world.TextInstances.Layouts.GetValue(Entity);

            // First, we need to catch up (i.e. fully reveal the glyphs
            // that come before the one we're supposed to be animating at this time).
            ushort nbCharsToReveal = (ushort)((Elapsed / GlyphTime) - _offset);
            nbCharsToReveal = (ushort)Math.Min(nbCharsToReveal, textLayout.Glyphs.Count - Position);
            RevealSpan(textLayout, Position, nbCharsToReveal);
            _offset += nbCharsToReveal;

            if (Position < textLayout.Glyphs.Count)
            {
                float currentGlyphOpacity = (Elapsed % GlyphTime) / GlyphTime;
                ref LayoutGlyph glyph = ref textLayout.MutateGlyph(Position);
                SetOpacity(ref glyph, currentGlyphOpacity);
            }

            if (HasCompleted)
            {
                IsAllTextVisible = true;
            }
        }

        private void RevealSpan(TextLayout textLayout, ushort start, ushort length)
        {
            Span<LayoutGlyph> span = textLayout.MutateSpan(start, length);
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
            glyph.Color.SetAlpha(opacity);
        }

        public void Stop()
        {
            TextLayout layout = _world.TextInstances.Layouts.GetValue(Entity);
            ref LayoutGlyph glyph = ref layout.MutateGlyph(Position);
            Reveal(ref glyph);
            IsAllTextVisible = Position == (layout.Glyphs.Count - 1);

            _world.DeactivateBehavior(this);
        }
    }
}
