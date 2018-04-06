using NitroSharp.Dialogue;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Objects;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Symbols;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        private const float TextRightMargin = 200;
        private const string VoiceEntityName = "__VOICE";

        private DialogueBlockSymbol _currentDialogueBlock;

        private FontFamily _currentFontFamily;
        private FontService FontService => _game.FontService;

        public Entity TextEntity { get; private set; }

        public override void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
        {
            var box = new RectangleVisual(width, height, RgbaFloat.White, 0.0f, 0);
            box.IsEnabled = false;

            _entities.Create(entityName, replace: true)
                .WithComponent(box)
                .WithPosition(x, y);

            _currentFontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
        }

        protected override void OnDialogueBlockEntered(DialogueBlockSymbol dialogueBlock)
        {
            _currentDialogueBlock = dialogueBlock;
            TextEntity?.Destroy();
        }

        public override void DisplayDialogue(string pxmlString)
        {
            if (_entities.TryGet(_currentDialogueBlock.AssociatedBox, out var dialogueBox))
            {
                var boxBounds = dialogueBox.Transform.Dimensions;
                var bounds = new Size((uint)boxBounds.X - (uint)TextRightMargin, (uint)boxBounds.Y);

                var dialogueLine = DialogueLine.Parse(pxmlString);
                var text = dialogueLine.Text;

                var textLayout = new TextLayout(text, dialogueLine.TextLength, _currentFontFamily, bounds);
                TextEntity = _entities.Create(_currentDialogueBlock.Identifier, replace: true)
                    .WithParent(dialogueBox)
                    .WithComponent(textLayout);

                TextEntity.Transform.Position.Y += 10;
            }

            WaitingForInput = true;
        }
    }
}
