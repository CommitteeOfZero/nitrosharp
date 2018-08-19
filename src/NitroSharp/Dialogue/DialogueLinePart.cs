namespace NitroSharp.Dialogue
{
    internal abstract class DialogueLinePart
    {
        public abstract DialogueLinePartKind PartKind { get; }
    }

    internal enum DialogueLinePartKind
    {
        Text,
        Voice,
        Marker
    }
}
