using System.IO;

namespace NitroSharp.NsScript.Syntax.Markup
{
    public class MarkupWriter : MarkupNodeVisitor
    {
        private readonly TextWriter _textWriter;

        public MarkupWriter()
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
