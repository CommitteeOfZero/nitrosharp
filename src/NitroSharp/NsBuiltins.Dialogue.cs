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
        private ref DialogueState _dialogueState => ref _world._dialogueState;
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
            _dialogueState.FontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
        }

        protected override void BeginDialogueBlock(DialogueBlockSymbol dialogueBlock)
        {
            ref DialogueState state = ref _dialogueState;
            state.DialogueBlock = dialogueBlock;
            if (_world.TryGetEntity(dialogueBlock.AssociatedBox, out Entity dialogueBox))
            {
                if (state.LastBlockName != null)
                {
                    _world.RemoveEntity(state.LastBlockName);
                }

                SizeF boxSize = _world.Rectangles.Bounds.GetValue(dialogueBox);
                const float TextRightMargin = 200;
                var layoutBounds = new Size((uint)boxSize.Width - (uint)TextRightMargin, (uint)boxSize.Height);
                var textLayout = new TextLayout(state.FontFamily, layoutBounds, 256);

                RgbaFloat color = RgbaFloat.White;
                string name = state.DialogueBlock.Identifier;
                state.LastBlockName = name;
                Entity text = _world.CreateTextInstance(name, textLayout, 99999, ref color);
                SetParent(text, dialogueBox);
                state.TextEntity = text;
            }
        }

        public override void BeginDialogueLine(string pxmlString)
        {
            ref DialogueState state = ref _dialogueState;
            state.Reset();
            state.DialogueLine = DialogueLine.Parse(pxmlString);

            //double iconX = Interpreter.Globals.Get("SYSTEM_position_x_text_icon").DoubleValue;
            //double iconY = Interpreter.Globals.Get("SYSTEM_position_y_text_icon").DoubleValue;
            //OldEntity pageIndicator = state.PageIndicator;
            //pageIndicator.Transform.Position = new Vector3((float)iconX, (float)iconY, 0);
            //pageIndicator.Visual.IsEnabled = true;
        }

        private void Voice(Voice voice)
        {
            //_dialogueState.Voice = voice;
            //if (voice.Action == VoiceAction.Play)
            //{
            //    var audio = new MediaComponent(Content.Get<MediaPlaybackSession>("voice/" + voice.FileName), AudioSourcePool);
            //    _world.Create(VoiceEnityName, replace: true).WithComponent(audio);
            //}
            //else
            //{
            //    _world.Remove(voice.FileName);
            //}
        }
    }
}
