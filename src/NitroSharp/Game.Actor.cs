using NitroSharp.Animation;
using NitroSharp.Experimental;
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

            protected abstract void HandleMessages(Queue<Message> messages);

            public void SyncTo(Actor actor)
            {
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
            FramebufferCaptured,

            // ScriptRunner -> Presenter
            BeginDialogueBlock,
            BeginDialogueLine,
            PresentDialogue,
            CaptureFramebuffer
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
            public Entity TextEntity { get; set; }
        }

        internal sealed class PresentDialogueMessage : Message
        {
            public override MessageKind Kind => MessageKind.PresentDialogue;
            public Dialogue.TextBuffer TextBuffer { get; set; }
        }
    }
}
