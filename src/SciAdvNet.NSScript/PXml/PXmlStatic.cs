using System;
using System.Collections.Generic;
using System.Text;

namespace SciAdvNet.NSScript.PXml
{
    public static class PXmlStatic
    {
        public static PXmlContent ParsePXmlContent(string text)
        {
            var parser = new PXmlParser(text);
            return parser.Parse();
        }
    }
}
