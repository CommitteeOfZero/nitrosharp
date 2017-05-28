using CommitteeOfZero.Nitro.Dialogue;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation;
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
                //Width = width,
                //Height = height
            };

            _entities.Create(entityName)
                .WithComponent(box)
                .WithPosition(x, y);
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
                    IsEnabled = true,
                    Priority = int.MaxValue,
                    Color = RgbaValueF.White
                };

                TextEntity = _entities.Create(_currentParagraph.Identifier, replace: true)
                    .WithParent(boxEntity)
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
