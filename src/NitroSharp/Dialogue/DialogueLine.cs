using System.Collections.Immutable;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.PXml;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Dialogue
{
    internal sealed class DialogueLine
    {
        private static readonly PXmlTreeVisitor s_treeVisitor = new PXmlTreeVisitor();

        public DialogueLine(ImmutableArray<DialogueLinePart> parts, Voice? voice, uint textLength)
        {
            Parts = parts;
            Voice = voice;
            TextLength = textLength;
        }

        public ImmutableArray<DialogueLinePart> Parts { get; }
        public Voice? Voice { get; }
        public uint TextLength { get; }
        public bool IsEmpty => Parts.Length == 0;

        public static DialogueLine Parse(string pxmlString)
        {
            var tree = Parsing.ParsePXmlString(pxmlString);
            return s_treeVisitor.ProduceDialogueLine(tree);
        }

        private sealed class PXmlTreeVisitor : PXmlSyntaxVisitor
        {
            private struct TextParams
            {
                public int? FontSize;
                public RgbaFloat? Color;
                public RgbaFloat? ShadowColor;
                public FontStyle FontStyle;
            }

            private readonly ImmutableArray<DialogueLinePart>.Builder _parts;
            private TextParams _textParams;
            private uint _textLength;
            private Voice? _voice;

            public PXmlTreeVisitor()
            {
                _parts = ImmutableArray.CreateBuilder<DialogueLinePart>(4);
            }

            public DialogueLine ProduceDialogueLine(PXmlNode treeRoot)
            {
                _parts.Clear();
                _textLength = 0;
                _voice = null;
                Visit(treeRoot);
                return new DialogueLine(_parts.ToImmutable(), _voice, _textLength);
            }

            public override void VisitVoiceElement(VoiceElement node)
            {
                _voice = new Voice(node.CharacterName, node.FileName, node.Action);
                _parts.Add(_voice);
            }

            public override void VisitContent(PXmlContent content)
            {
                VisitArray(content.Children);
            }

            public override void VisitFontElement(FontElement fontElement)
            {
                TextParams oldParams = _textParams;
                _textParams.FontSize = fontElement.Size;
                if (fontElement.Color.HasValue)
                {
                    NsColor color = fontElement.Color.Value;
                    _textParams.Color = color.ToRgbaFloat();
                }

                Visit(fontElement.Content);
                _textParams = oldParams;
            }

            public override void VisitText(PXmlText text)
            {
                if (text.Text.Length > 0)
                {
                    var textRun = new TextRun
                    {
                        Text = text.Text,
                        FontSize = _textParams.FontSize,
                        Color = _textParams.Color,
                        ShadowColor = _textParams.ShadowColor
                    };

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
