using NitroSharp.NsScript;
using NitroSharp.NsScript.Symbols;
using Veldrid;
using NitroSharp.Logic.Objects;

namespace NitroSharp
{
    internal sealed partial class NitroCore
    {
        private const float TextRightMargin = 150;
        private const string VoiceEntityName = "__VOICE";

        private DialogueBlockSymbol _currentParagraph;

        public Entity TextEntity { get; private set; }

        public override void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
        {
            var box = new RectangleVisual(width, height, RgbaFloat.White, 0.0f, 0);
            box.IsEnabled = false;

            _entities.Create(entityName, replace: true)
                .WithComponent(box)
                .WithPosition(x, y);
        }

        protected override void OnDialogueBlockEntered(DialogueBlockSymbol dialogueBlock)
        {
            _currentParagraph = dialogueBlock;
            TextEntity?.Destroy();
        }

        public override void DisplayDialogue(string pxmlString)
        {
        }
    }
}
