using System;
using System.Numerics;
using NitroSharp.Animation;
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

        private DialogueBlockSymbol _currentDialogueBlock;
        internal Entity TextEntity;
        private Entity _pageIndicator;

        private FontFamily _currentFontFamily;
        private FontService FontService => _game.FontService;

        private void LoadPageIndicator()
        {
            var visual = PageIndicator.Load(Content, "cg/sys/icon/page");
            visual.IsEnabled = false;

            var duration = TimeSpan.FromMilliseconds(visual.IconCount * 128);
            var animation = new UIntAnimation<PageIndicator>(
                visual, (i, v) => i.ActiveIconIndex = v, 0, visual.IconCount - 1,
                duration, TimingFunction.Linear, repeat: true);

            _pageIndicator = _entities.Create("__PAGE_INDICATOR")
                .WithComponent(visual)
                .WithComponent(animation);

            RequestCore(_pageIndicator, NsEntityAction.Lock);
        }

        public override void CreateDialogueBox(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y, int width, int height)
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
                    .WithComponent(textLayout)
                    .WithComponent(new TextRevealAnimation());

                TextEntity.Transform.Position.Y += 10;

                double iconX = Interpreter.Globals.Get("SYSTEM_position_x_text_icon").DoubleValue;
                double iconY = Interpreter.Globals.Get("SYSTEM_position_y_text_icon").DoubleValue;
                _pageIndicator.Transform.Position = new Vector3((float)iconX, (float)iconY, 0);
                _pageIndicator.Visual.IsEnabled = true;
            }

            WaitingForInput = true;
        }
    }
}
