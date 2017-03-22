using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy
{
    public class TextInputHandler : EntityProcessingSystem
    {
        public TextInputHandler() : base(typeof(TextComponent))
        {

        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var text = entity.GetComponent<TextComponent>();
            if (Mouse.IsButtonDownThisFrame(MouseButton.Left))
            {
                text.Animated = false;
            }
        }
    }
}
