namespace NitroSharp.Text
{
    internal sealed class FontFamily
    {
        private readonly FontService _fontService;

        public FontFamily(string name, FontService fontService)
        {
            Name = name;
            _fontService = fontService;
        }

        public string Name { get; }

        public FontFace GetFace(FontStyle style)
        {
            return _fontService.Find(Name, style);
        }
    }
}
