using MoeGame.Framework;
using CommitteeOfZero.Nitro.Graphics.RenderItems;
using System;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class TypewriterAnimationProcessor : EntityProcessingSystem
    {
        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(GameTextVisual));
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var text = entity.GetComponent<GameTextVisual>();
            if (text.CurrentGlyphIndex >= text.Text?.Length || string.IsNullOrEmpty(text.Text))
            {
                return;
            }

            text.CurrentGlyphOpacity += 1.0f * (deltaMilliseconds / 80.0f);

            if (text.CurrentGlyphOpacity >= 1.0f || text.Text[text.CurrentGlyphIndex] == ' ')
            {
                text.CurrentGlyphOpacity = 0.0f;
                text.CurrentGlyphIndex++;
            }
        }
    }
}
