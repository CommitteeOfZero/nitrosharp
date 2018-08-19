namespace NitroSharp.Dialogue
{
    internal sealed class Voice : DialogueLinePart
    {
        public Voice(string characterName, string fileName, VoiceAction action)
        {
            CharacterName = characterName;
            FileName = fileName;
            Action = action;
        }

        public string CharacterName { get; }
        public string FileName { get; }
        public VoiceAction Action { get; }

        public override DialogueLinePartKind PartKind => DialogueLinePartKind.Voice;
    }

    internal enum VoiceAction
    {
        Play,
        Stop
    }
}
