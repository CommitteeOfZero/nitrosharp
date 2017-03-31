using HoppyFramework;
using ProjectHoppy.Text;

namespace ProjectHoppy.Graphics
{
    public class TypewriterAnimationProcessor : EntityProcessingSystem
    {
        public TypewriterAnimationProcessor()
            : base(typeof(TextComponent))
        {
            
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var text = entity.GetComponent<TextComponent>();
            if (text.CurrentGlyphIndex >= text.Text.Length)
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
