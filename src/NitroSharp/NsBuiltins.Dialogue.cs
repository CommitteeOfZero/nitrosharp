using System;
using NitroSharp.Dialogue;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Symbols;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private string _lastDialogueBlockName;
        private FontFamily _fontFamily;

        private FontService FontService => _game.FontService;

        public override void CreateDialogueBox(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y, int width, int height)
        {
            // TODO: remove the hardcoded text color.

            RgbaFloat color = RgbaFloat.White;
            color.SetAlpha(0);
            Entity box = _world.CreateRectangle(entityName, priority, new SizeF(width, height), ref color);
            SetPosition(box, x, y);

            // TODO: move this line to SetFont.
            _fontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
        }

        protected override void BeginDialogueBlock(DialogueBlockSymbol dialogueBlock)
        {
            if (_world.TryGetEntity(dialogueBlock.AssociatedBox, out Entity dialogueBox))
            {
                if (_lastDialogueBlockName != null)
                {
                    _world.RemoveEntity(_lastDialogueBlockName);
                }

                SizeF boxSize = _world.Rectangles.Bounds.GetValue(dialogueBox);
                const float TextRightMargin = 200;
                var layoutBounds = new Size((uint)boxSize.Width - (uint)TextRightMargin, (uint)boxSize.Height);
                var textLayout = new TextLayout(_fontFamily, layoutBounds, 256);

                RgbaFloat color = RgbaFloat.White;
                string name = dialogueBlock.Identifier;
                _lastDialogueBlockName = name;
                Entity text = _world.CreateTextInstance(name, textLayout, 99999, ref color);
                SetParent(text, dialogueBox);

                _game.MessageQueue.Enqueue(new BeginDialogueBlockMessage
                {
                    DialogueBlock = dialogueBlock,
                    TextEntity = text
                });
            }
        }

        public override void BeginDialogueLine(string pxmlString)
        {
            _game.MessageQueue.Enqueue(new BeginDialogueLineMessage
            {
                DialogueLine = DialogueLine.Parse(pxmlString)
            });

            //double iconX = Interpreter.Globals.Get("SYSTEM_position_x_text_icon").DoubleValue;
            //double iconY = Interpreter.Globals.Get("SYSTEM_position_y_text_icon").DoubleValue;
            //OldEntity pageIndicator = state.PageIndicator;
            //pageIndicator.Transform.Position = new Vector3((float)iconX, (float)iconY, 0);
            //pageIndicator.Visual.IsEnabled = true;
        }

        public override void LoadText(string boxName, string textName, int maxWidth, int maxHeight, int letterSpacing, int lineSpacing)
        {
        }
    }
}
