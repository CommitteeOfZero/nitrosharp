using System.IO;

namespace NitroSharp.NsScript.Syntax.PXml
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
