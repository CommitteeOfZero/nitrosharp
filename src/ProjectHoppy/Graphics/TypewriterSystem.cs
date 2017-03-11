using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ProjectHoppy.Graphics
{
    public class TypewriterSystem : EntityProcessingSystem
    {
        public TypewriterSystem() : base(typeof(TextComponent))
        {
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var text = entity.GetComponent<TextComponent>();
            //Thread.Sleep(16);

            text.CurrentGlyphColor.A += 1;
            text.CurrentGlyphColor.R += 1;
            text.CurrentGlyphColor.G += 1;
            text.CurrentGlyphColor.B += 1;
        }
    }
}
