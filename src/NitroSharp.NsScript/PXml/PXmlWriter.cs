using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NitroSharp.NsScript.PXml
{
    public class PXmlWriter : PXmlSyntaxVisitor
    {
        private readonly TextWriter _textWriter;

        public PXmlWriter()
        {
            _textWriter = new StringWriter();
        }

        public override void VisitVoiceElement(VoiceElement voiceElement)
        {

        }

        private void Write(string s)
        {
            _textWriter.Write(s);
        }
    }
}
