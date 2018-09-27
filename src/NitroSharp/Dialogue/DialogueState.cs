using NitroSharp.NsScript.Symbols;
using NitroSharp.Text;

namespace NitroSharp.Dialogue
{
    internal struct DialogueState
    {
        public enum CommandKind
        {
            NoOp,
            Begin
        }

        public DialogueBlockSymbol DialogueBlock;
        public DialogueLine DialogueLine;
        public int CurrentDialoguePart;
        public FontFamily FontFamily;
        public Entity TextEntity;
        public bool StartFromNewLine;
        public bool Clear;

        public TextRevealAnimation RevealAnimation;
        public RevealSkipAnimation RevealSkipAnimation;
        public Voice Voice;

        public CommandKind Command;
        public string LastBlockName;
        internal string LastVoiceName;

        public bool CanAdvance
        {
            get => DialogueLine != null && CurrentDialoguePart < DialogueLine.Parts.Length;
        }

        public void Reset()
        {
            DialogueLine = null;
            CurrentDialoguePart = 0;
            StartFromNewLine = false;
            Command = CommandKind.Begin;
        }
    }
}
