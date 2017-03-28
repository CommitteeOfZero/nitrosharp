namespace SciAdvNet.NSScript.PXml
{
    public static class PXmlBlock
    {
        public static PXmlContent Parse(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
