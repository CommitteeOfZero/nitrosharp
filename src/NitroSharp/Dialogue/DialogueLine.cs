using System.Collections.Immutable;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.PXml;
using NitroSharp.Primitives;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class DialogueLine
    {
        private static readonly PXmlTreeVisitor s_treeVisitor = new PXmlTreeVisitor();

        public DialogueLine(ImmutableArray<DialogueLinePart> parts, uint textLength)
        {
            Parts = parts;
            TextLength = textLength;
        }

        public ImmutableArray<DialogueLinePart> Parts { get; }
        public uint TextLength { get; }

        public static DialogueLine Parse(string pxmlString)
        {
            var tree = Parsing.ParsePXmlString(pxmlString);
            return s_treeVisitor.ProduceDialogueLine(tree);
        }

        private sealed class PXmlTreeVisitor : PXmlSyntaxVisitor
        {
            private readonly ImmutableArray<DialogueLinePart>.Builder _parts;
            private TextRun _textParams = new TextRun();
            private uint _textLength;

            public PXmlTreeVisitor()
            {
                _parts = ImmutableArray.CreateBuilder<DialogueLinePart>(4);
            }

            public DialogueLine ProduceDialogueLine(PXmlNode treeRoot)
            {
                _parts.Clear();
                _textLength = 0;
                Visit(treeRoot);
                return new DialogueLine(_parts.ToImmutable(), _textLength);
            }

            public override void VisitVoiceElement(VoiceElement node)
            {
                _parts.Add(new Voice(node.CharacterName, node.FileName, (VoiceAction)node.Action));
            }

            public override void VisitContent(PXmlContent content)
            {
                VisitArray(content.Children);
            }

            public override void VisitFontElement(FontElement fontElement)
            {
                TextRun old = _textParams;
                _textParams.FontSize = fontElement.Size;
                if (fontElement.Color.HasValue)
                {
                    NsColor color = fontElement.Color.Value;
                    _textParams.Color = color.ToRgbaFloat();
                }

                Visit(fontElement.Content);
                _textParams = old;
            }

            public override void VisitText(PXmlText text)
            {
                if (text.Text.Length > 0)
                {
                    var textRun = new TextRun();
                    textRun.Text = text.Text;
                    textRun.FontSize = _textParams.FontSize;
                    textRun.Color = _textParams.Color;
                    textRun.ShadowColor = _textParams.ShadowColor;

                    _parts.Add(new TextPart(textRun));
                    _textLength += (uint)text.Text.Length;
                }
            }

            public override void VisitHaltElement(HaltElement haltElement)
            {
                _parts.Add(new Marker(MarkerKind.Halt));
            }

            public override void VisitNoLinebreaksElement(NoLinebreaksElement element)
            {
                _parts.Add(new Marker(MarkerKind.NoLinebreaks));
            }
        }
    }
}
