using NitroSharp.NsScript;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Dialogue
{
    internal enum DialogueLinePartKind
    {
        Text,
        Voice,
        Marker
    }

    internal abstract class DialogueLinePart
    {
        public abstract DialogueLinePartKind PartKind { get; }
    }

    internal sealed class TextPart : DialogueLinePart
    {
        public TextPart(TextRun textRun)
        {
            Text = textRun;
        }

        public TextRun Text { get; }
        public override DialogueLinePartKind PartKind => DialogueLinePartKind.Text;
    }

    internal sealed class Voice : DialogueLinePart
    {
        public Voice(string characterName, string fileName, NsVoiceAction action)
        {
            CharacterName = characterName;
            FileName = fileName;
            Action = action;
        }

        public string CharacterName { get; }
        public string FileName { get; }
        public NsVoiceAction Action { get; }

        public override DialogueLinePartKind PartKind => DialogueLinePartKind.Voice;
    }

    internal enum MarkerKind
    {
        Halt,
        NoLinebreaks
    }

    internal sealed class Marker : DialogueLinePart
    {
        public Marker(MarkerKind kind)
        {
            MarkerKind = kind;
        }

        public MarkerKind MarkerKind { get; }
        public override DialogueLinePartKind PartKind => DialogueLinePartKind.Marker;
    }
}
