namespace NitroSharp.Dialogue
{
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
