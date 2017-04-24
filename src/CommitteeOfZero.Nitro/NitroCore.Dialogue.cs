using CommitteeOfZero.Nitro.Graphics.Visuals;
using CommitteeOfZero.Nitro.Dialogue;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.NsScript.PXml;
using MoeGame.Framework;
using System.Numerics;
using System.Threading.Tasks;
using CommitteeOfZero.Nitro.Audio;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        private readonly PXmlTreeFlattener _pxmlFlattener = new PXmlTreeFlattener();
        private DialogueBox _currentDialogueBox;
        private Entity _textEntity;

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

        private void OnEnteredDialogueBlock(object sender, DialogueBlock block)
        {
            if (_textEntity != null)
            {
                _entities.Remove(_textEntity);
            }

            _entities.TryGet(block.BoxName, out var boxEntity);
            _currentDialogueBox = boxEntity?.GetComponent<DialogueBox>();

            var textVisual = new GameTextVisual
            {
                Position = _currentDialogueBox.Position,
                Width = _currentDialogueBox.Width,
                Height = _currentDialogueBox.Height,
                IsEnabled = false
            };

            _textEntity = _entities.Create(block.Identifier, replace: true).WithComponent(textVisual);
        }

        public override void DisplayDialogue(string pxmlString)
        {
            CurrentThread.Suspend();

            Task.Run(() => ParseDialogueLine(pxmlString))
                .ContinueWith(t => DisplayDialogueCore(t.Result), _game.MainLoopTaskScheduler);
        }

        private DialogueLine ParseDialogueLine(string pxmlString)
        {
            var root = PXmlBlock.Parse(pxmlString);
            return _pxmlFlattener.Flatten(root);
        }

        private void DisplayDialogueCore(DialogueLine dialogueLine)
        {
            _entities.TryGet(CurrentDialogueBlock.Identifier, out var textEntity);
            var textVisual = textEntity?.GetComponent<GameTextVisual>();
            if (textVisual != null)
            {
                textVisual.Reset();

                textVisual.Text = dialogueLine.Text;
                textVisual.Priority = 30000;
                textVisual.Color = RgbaValueF.White;
                textVisual.IsEnabled = true;
            }

            if (dialogueLine.Voice != null)
            {
                VoiceAction(dialogueLine.Voice);
            }
        }

        private Entity _voiceEntity;
        private void VoiceAction(Voice voice)
        {
            if (voice.Action == Dialogue.VoiceAction.Play)
            {
                if (_voiceEntity != null)
                {
                    _entities.Remove(_voiceEntity);
                }

                var sound = new SoundComponent
                {
                    AudioFile = "voice/" + voice.FileName,
                    Volume = 1.0f,
                    Kind = AudioKind.Voice
                };

                _voiceEntity = _entities.Create(voice.FileName).WithComponent(sound);
            }
            else
            {
                _entities.Remove(voice.FileName);
            }
        }
    }
}
