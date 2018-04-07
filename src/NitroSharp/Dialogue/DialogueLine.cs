using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.PXml;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Dialogue
{
    internal sealed class DialogueLine
    {
        private static readonly PXmlTreeVisitor s_treeVisitor = new PXmlTreeVisitor();

        private DialogueLine(TextRun[] text, uint textLength, in Voice voice)
        {
            Text = text;
            TextLength = textLength;
            Voice = voice;
        }

        public TextRun[] Text { get; }
        public uint TextLength { get; }
        public Voice Voice { get; }

        public static DialogueLine Parse(string pxmlString)
        {
            var tree = Parsing.ParsePXmlString(pxmlString);
            return s_treeVisitor.ProduceDialogueLine(tree);
        }

        private sealed class PXmlTreeVisitor : PXmlSyntaxVisitor
        {
            private TextRun _currentRun = new TextRun();
            private ValueList<TextRun> _textRuns = new ValueList<TextRun>(2);
            private Voice _voice;
            private uint _textLength;

            public DialogueLine ProduceDialogueLine(PXmlNode treeRoot)
            {
                _textRuns.Reset();
                _textLength = 0;
                Visit(treeRoot);
                return new DialogueLine(_textRuns.ToArray(), _textLength, _voice);
            }

            public override void VisitVoiceElement(VoiceElement node)
            {
                _voice = new Voice(node.CharacterName, node.FileName, (VoiceAction)node.Action);
            }

            public override void VisitContent(PXmlContent content)
            {
                VisitArray(content.Children);
            }

            public override void VisitFontElement(FontElement fontElement)
            {
                if (fontElement.Color.HasValue)
                {
                    var val = fontElement.Color.Value;
                    _currentRun.Color = new RgbaFloat(val.R / 255.0f, val.G / 255.0f, val.B / 255.0f, 1.0f);
                }

                _currentRun.FontSize = fontElement.Size;

                var copy = _currentRun;
                Visit(fontElement.Content);
                _currentRun = copy;
            }

            public override void VisitText(PXmlText text)
            {
                ref var current = ref _currentRun;
                ref var span = ref _textRuns.Add();
                span.Text = text.Text;
                span.FontSize = current.FontSize;
                span.Color = current.Color;
                span.ShadowColor = current.ShadowColor;

                _textLength += (uint)text.Text.Length;
            }
        }
    }
}
