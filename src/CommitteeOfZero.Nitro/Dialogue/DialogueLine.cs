namespace CommitteeOfZero.Nitro.Dialogue
{
    public sealed class DialogueLine
    {
        public DialogueLine(string text, Voice voice)
        {
            Text = text;
            Voice = voice;
        }

        public string Text { get; }
        public Voice Voice { get; }
    }
}
