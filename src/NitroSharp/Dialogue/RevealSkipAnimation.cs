using System;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Primitives;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class RevealSkipAnimation : AnimationBase
    {
        private readonly ushort _skipStart;

        public RevealSkipAnimation(Entity textEntity, ushort skipStartPosition)
            : base(textEntity, TimeSpan.FromMilliseconds(200))
        {
            _skipStart = skipStartPosition;
        }

        protected override void Advance(World world, float deltaMilliseconds)
        {
            TextLayout textLayout = world.TextInstances.Layouts.GetValue(Entity);
            uint length = textLayout.Glyphs.Count - _skipStart;
            Span<LayoutGlyph> span = textLayout.MutateSpan(_skipStart, (ushort)length);
            for (int i = 0; i < span.Length; i++)
            {
                SetOpacity(ref span[i], Progress);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOpacity(ref LayoutGlyph glyph, float opacity)
        {
            glyph.Color.SetAlpha(opacity);
        }
    }
}
