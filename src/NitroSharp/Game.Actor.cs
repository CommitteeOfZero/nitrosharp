using NitroSharp.Animation;
using NitroSharp.Dialogue;
using NitroSharp.NsScript.Symbols;
using System.Collections.Generic;

namespace NitroSharp
{
    public partial class Game
    {
        internal abstract class Actor
        {
            private readonly Queue<Message> _messageQueue;

            protected Actor(World world)
            {
                World = world;
                _messageQueue = new Queue<Message>();
            }

            public World World { get; }

            public void PostMessage(Message message)
            {
                _messageQueue.Enqueue(message);
            }

            protected abstract void HandleMessages<T>(T messages) where T : IEnumerable<Message>;

            public void SyncTo(Actor actor)
            {
                if (!ReferenceEquals(World, actor.World))
                {
                    actor.World.MergeChangesInto(World);
                }

                Queue<Message> messages = actor._messageQueue;
                HandleMessages(messages);
                messages.Clear();
            }
        }

        internal enum MessageKind
        {
            // Presenter -> ScriptRunner
            ResumeMainThread,
            SuspendMainThread,
            ThreadAction,
            AnimationCompleted,
            ChoiceSelected,

            // ScriptRunner -> Presenter
            BeginDialogueBlock,
            BeginDialogueLine
        }

        internal abstract class Message
        {
            public abstract MessageKind Kind { get; }
        }

        internal sealed class SimpleMessage : Message
        {
            public SimpleMessage(MessageKind kind)
            {
                Kind = kind;
            }

            public override MessageKind Kind { get; }
        }

        internal sealed class ThreadActionMessage : Message
        {
            public enum ActionKind
            {
                StartOrResume,
                Terminate
            }

            public override MessageKind Kind => MessageKind.ThreadAction;
            public InterpreterThreadInfo ThreadInfo { get; set; }
            public ActionKind Action { get; set; }
        }

        internal sealed class ChoiceSelectedMessage : Message
        {
            public override MessageKind Kind => MessageKind.ChoiceSelected;
            public string ChoiceName { get; set; }
        }

        internal sealed class AnimationCompletedMessage : Message
        {
            public override MessageKind Kind => MessageKind.AnimationCompleted;
            public PropertyAnimation Animation { get; set; }
        }

        internal sealed class BeginDialogueBlockMessage : Message
        {
            public override MessageKind Kind => MessageKind.BeginDialogueBlock;
            public DialogueBlockSymbol DialogueBlock { get; set; }
            public Entity TextEntity { get; set; }
        }

        internal sealed class BeginDialogueLineMessage : Message
        {
            public override MessageKind Kind => MessageKind.BeginDialogueLine;
            public DialogueLine DialogueLine { get; set; }
        }
    }
}
