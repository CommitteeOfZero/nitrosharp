using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal sealed class TextPart : DialogueLinePart
    {
        public TextPart(TextRun textRun)
        {
            Text = textRun;
        }

        public TextRun Text { get; }
        public override DialogueLinePartKind PartKind => DialogueLinePartKind.TextPart;
    }
}
