namespace NitroSharp.Text
{
    internal sealed class FontFamily
    {
        private readonly FontLibrary _fontService;

        public FontFamily(string name, FontLibrary fontService)
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
