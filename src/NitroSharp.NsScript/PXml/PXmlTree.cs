namespace NitroSharp.NsScript.PXml
{
    public static class PXmlTree
    {
        public static PXmlContent ParseString(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
