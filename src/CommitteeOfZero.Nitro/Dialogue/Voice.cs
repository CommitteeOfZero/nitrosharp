namespace CommitteeOfZero.Nitro.Dialogue
{
    public sealed class Voice
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
    }
}
