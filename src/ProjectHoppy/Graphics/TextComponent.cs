namespace ProjectHoppy.Graphics
{
    public class TextComponent : Component
    {
        public string Text { get; set; }
        public bool Animated { get; set; }
        public float CurrentGlyphOpacity { get; set; }
        public int CurrentGlyphIndex { get; set; }
    }
}
