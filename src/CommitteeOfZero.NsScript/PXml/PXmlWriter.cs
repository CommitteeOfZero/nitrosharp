using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommitteeOfZero.NsScript.PXml
{
    public class PXmlWriter : PXmlSyntaxVisitor
    {
        private TextWriter _textWriter;

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
