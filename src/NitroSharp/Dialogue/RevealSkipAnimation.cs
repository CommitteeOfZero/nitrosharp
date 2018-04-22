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
        private readonly TextLayout _textLayout;
        private readonly uint _skipStart;

        public RevealSkipAnimation(TextLayout textLayout, uint skipStartPosition)
            : base(TimeSpan.FromMilliseconds(200))
        {
            _textLayout = textLayout;
            _skipStart = skipStartPosition;
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
