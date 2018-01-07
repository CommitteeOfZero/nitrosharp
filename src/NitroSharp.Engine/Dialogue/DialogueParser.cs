using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.PXml;
using System.Text;

namespace NitroSharp.Dialogue
{
    public static class DialogueParser
    {
        private static readonly TreeFlattener s_treeFlattener = new TreeFlattener();

        public static DialogueLine Parse(string pxmlString)
        {
            var root = Parsing.ParsePXmlString(pxmlString);
            return s_treeFlattener.Flatten(root);
        }

        private sealed class TreeFlattener : PXmlSyntaxVisitor
        {
            private readonly StringBuilder _builder;
            private Voice _currentVoice;

            public TreeFlattener()
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

            public override void VisitColorElement(ColorElement fontColorElement)
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
}
