using CommitteeOfZero.NsScript.PXml;
using System.Text;

namespace CommitteeOfZero.Nitro.Dialogue
{
    public class PXmlTreeFlattener : PXmlSyntaxVisitor
    {
        private readonly StringBuilder _builder;
        private Voice _currentVoice;

        public PXmlTreeFlattener()
        {
            _builder = new StringBuilder();
        }

        public DialogueLine Flatten(PXmlContent treeRoot)
        {
            _builder.Clear();
            _currentVoice = null;

            Visit(treeRoot);

            string text = _builder.ToString();
            var voice = _currentVoice;
            return new DialogueLine(text, voice);
        }

        public override void VisitContent(PXmlContent content)
        {
            VisitArray(content.Children);
        }

        public override void VisitFontColorElement(FontColorElement fontColorElement)
        {
            Visit(fontColorElement.Content);
        }

        public override void VisitText(PXmlText text)
        {
            _builder.Append(text.Text);
        }

        public override void VisitVoiceElement(VoiceElement voiceElement)
        {
            _currentVoice = new Voice(voiceElement.CharacterName, voiceElement.FileName, (VoiceAction)voiceElement.Action);
        }
    }
}
