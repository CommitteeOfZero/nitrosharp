using SciAdvNet.MediaLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy.Graphics
{
    public class TextComponent : Component
    {
        public TextComponent()
        {
            CurrentGlyphColor = new Color(0, 0, 0, 0);
        }

        public string Text { get; set; }
        public Color CurrentGlyphColor;
    }
}
