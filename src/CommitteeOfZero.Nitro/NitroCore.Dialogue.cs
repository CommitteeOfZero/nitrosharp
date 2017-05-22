using CommitteeOfZero.Nitro.Dialogue;
using CommitteeOfZero.NsScript;
using MoeGame.Framework;
using System.Numerics;
using System.Threading.Tasks;
using CommitteeOfZero.Nitro.Audio;
using CommitteeOfZero.Nitro.Graphics;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        private Paragraph _currentParagraph;
        public Entity TextEntity { get; private set; }
        private Entity _voiceEntity;

        public override void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
        {
            var box = new DialogueBox
            {
                Position = Position(x, y, Vector2.Zero, width, height),
                Width = width,
                Height = height
            };

            _entities.Create(entityName).WithComponent(box);
        }

        protected override void OnParagraphEntered(Paragraph paragraph)
        {
            _currentParagraph = paragraph;
            TextEntity?.Destroy();
        }

        public override void DisplayDialogue(string pxmlString)
        {
            MainThread.Suspend();
            Task.Run(() => DialogueParser.Parse(pxmlString))
                .ContinueWith(t => DisplayDialogueCore(t.Result), _game.MainLoopTaskScheduler);
        }

        private void DisplayDialogueCore(DialogueLine dialogueLine)
        {
            if (_entities.TryGet(_currentParagraph.AssociatedBox, out var boxEntity))
            {
                var dialogueBox = boxEntity.GetComponent<DialogueBox>();
                var textVisual = new TextVisual(dialogueLine.Text)
                {
                    Position = dialogueBox.Position,
                    Width = dialogueBox.Width - 120,
                    Height = dialogueBox.Height,
                    IsEnabled = true,
                    Priority = int.MaxValue,
                    Color = RgbaValueF.White
                };

                TextEntity = _entities.Create(_currentParagraph.Identifier, replace: true)
                    .WithComponent(textVisual)
                    .WithComponent(new SmoothTextAnimation());

                if (dialogueLine.Voice != null)
                {
                    VoiceAction(dialogueLine.Voice);
                }
            }
        }

        private void VoiceAction(Voice voice)
        {
            if (voice.Action == Dialogue.VoiceAction.Play)
            {
                _voiceEntity?.Destroy();
                var sound = new SoundComponent("voice/" + voice.FileName, AudioKind.Voice);
                _voiceEntity = _entities.Create(voice.FileName).WithComponent(sound);
            }
            else
            {
                _entities.Remove(voice.FileName);
            }
        }
    }
}
