namespace NitroSharp.Text
{
    internal readonly struct FontMetrics
    {
        public FontMetrics(float ascender, float descender, float lineHeight)
        {
            Ascender = ascender;
            Descender = descender;
            LineHeight = lineHeight;
        }

        public readonly float Ascender;
        public readonly float Descender;
        public readonly float LineHeight;
    }
}
