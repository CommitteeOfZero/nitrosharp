using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Graphics.Objects;
using NitroSharp.Primitives;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class RevealSkipAnimation : AnimationBase
    {
        private readonly uint _skipStart;
        private TextLayout _textLayout;

        public RevealSkipAnimation(uint skipStartPosition) : base(TimeSpan.FromMilliseconds(200))
        {
            _skipStart = skipStartPosition;
        }

        public override void OnAttached()
        {
            var textLayout = Entity.GetComponent<TextLayout>();
            if (textLayout == null)
            {
                throw new InvalidOperationException("This component can't be attached to an entity that doesn't have a TextLayout component.");
            }

            _textLayout = textLayout;
        }

        public override void Advance(float deltaMilliseconds)
        {
            base.Advance(deltaMilliseconds);

            uint length = _textLayout.GlyphCount - _skipStart;
            var span = _textLayout.MutateSpan(_skipStart, length);
            for (int i = 0; i < span.Length; i++)
            {
                SetOpacity(ref span[i], Progress);
            }

            PostAdvance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOpacity(ref LayoutGlyph glyph, float opacity)
        {
            glyph.Color = glyph.Color.SetAlpha(opacity);
        }
    }
}
