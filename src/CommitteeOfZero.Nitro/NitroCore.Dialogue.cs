using CommitteeOfZero.Nitro.Graphics.Visuals;
using CommitteeOfZero.Nitro.Text;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.NsScript.PXml;
using MoeGame.Framework;
using System.Numerics;
using System.Threading.Tasks;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
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

            Task.Run(() =>
            {
                var root = PXmlBlock.Parse(pxmlString);
                var flattener = new PXmlTreeFlattener();

                string plainText = flattener.Flatten(root);
                return Task.FromResult(plainText);
            }).ContinueWith(t =>
            {
                string plainText = t.Result;
                _entities.TryGet(CurrentDialogueBlock.Identifier, out var textEntity);
                var textVisual = textEntity?.GetComponent<GameTextVisual>();
                if (textVisual != null)
                {
                    textVisual.Reset();

                    textVisual.Text = plainText;
                    textVisual.Priority = 30000;
                    textVisual.Color = RgbaValueF.White;
                    textVisual.IsEnabled = true;
                }
            }, _game.MainLoopTaskScheduler);
        }
    }
}
