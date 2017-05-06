using MoeGame.Framework;
using System;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TypewriterAnimationProcessor : EntityProcessingSystem
    {
        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(TextVisual));
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var text = entity.GetComponent<TextVisual>();
            int idxAnimatedGlyph = text.AnimatedRegion.RangeStart;
            if (idxAnimatedGlyph >= text.Text?.Length || string.IsNullOrEmpty(text.Text))
            {
                return;
            }

            text.AnimatedOpacity += 1.0f * (deltaMilliseconds / 80.0f);

            if (text.AnimatedOpacity >= 1.0f || text.Text[idxAnimatedGlyph] == ' ')
            {
                text.AnimatedOpacity = 0.0f;
                text.AnimatedRegion = new TextRange(text.AnimatedRegion.RangeStart + 1, 1);
                text.VisibleRegion = new TextRange(text.VisibleRegion.RangeStart, text.VisibleRegion.Length + 1);
            }
        }
    }
}
