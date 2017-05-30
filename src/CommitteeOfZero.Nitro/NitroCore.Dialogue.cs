using CommitteeOfZero.Nitro.Dialogue;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation;
using System.Threading.Tasks;
using CommitteeOfZero.Nitro.Audio;
using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.Nitro.Foundation.Content;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        private const float TextRightMargin = 120;

        private Paragraph _currentParagraph;
        public Entity TextEntity { get; private set; }
        private Entity _voiceEntity;

        public override void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
        {
            var box = new RectangleVisual(width, height, RgbaValueF.White, 0.0f, 0);
            box.IsEnabled = false;

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
            if (_entities.TryGet(_currentParagraph.AssociatedBox, out var dialogueBox))
            {
                var bounds = dialogueBox.Transform.Bounds;
                bounds = new System.Drawing.SizeF(bounds.Width - TextRightMargin, bounds.Height);

                var textVisual = new TextVisual(dialogueLine.Text, bounds, RgbaValueF.White, int.MaxValue);
                TextEntity = _entities.Create(_currentParagraph.Identifier, replace: true)
                    .WithParent(dialogueBox)
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
