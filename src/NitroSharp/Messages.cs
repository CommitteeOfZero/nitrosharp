using NitroSharp.Dialogue;
using NitroSharp.NsScript.Execution;
using NitroSharp.NsScript.Symbols;

namespace NitroSharp
{
    internal abstract class Message
    {
    }

    internal sealed class BeginDialogueBlockMessage : Message
    {
        public DialogueBlockSymbol DialogueBlock { get; set; }
        public Entity TextEntity { get; set; }
    }

    internal sealed class BeginDialogueLineMessage : Message
    {
        public DialogueLine DialogueLine { get; set; }
    }

    internal sealed class SelectChoiceMessage : Message
    {
        public ThreadContext WaitingThread { get; set; }
    }

    internal sealed class ChoiceSelectedMessage : Message
    {
        public string ChoiceName { get; set; }
    }
}
