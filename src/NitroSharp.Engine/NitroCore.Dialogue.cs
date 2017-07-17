﻿using NitroSharp.Dialogue;
using NitroSharp.NsScript;
using NitroSharp.Foundation;
using NitroSharp.Audio;
using NitroSharp.Graphics;
using NitroSharp.Foundation.Audio;

namespace NitroSharp
{
    public sealed partial class NitroCore
    {
        private const float TextRightMargin = 150;
        private const string VoiceEntityName = "__VOICE";

        private Paragraph _currentParagraph;
        private DialogueLine _currentDialogueLine;

        public Entity TextEntity { get; private set; }

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
            var line = DialogueParser.Parse(pxmlString);
            DisplayDialogueCore(line);
        }

        private void DisplayDialogueCore(DialogueLine dialogueLine)
        {
            if (string.IsNullOrEmpty(_currentDialogueLine?.Text) && _currentDialogueLine?.Voice != null)
            {
                dialogueLine.Voice = _currentDialogueLine.Voice;
            }

            if (_entities.TryGet(_currentParagraph.AssociatedBox, out var dialogueBox))
            {
                _currentDialogueLine = dialogueLine;
                var bounds = dialogueBox.Transform.Bounds;
                bounds = new System.Drawing.SizeF(bounds.Width - TextRightMargin, bounds.Height);

                if (!string.IsNullOrEmpty(dialogueLine.Text))
                {
                    var textVisual = new TextVisual(dialogueLine.Text, bounds, RgbaValueF.White, int.MaxValue);
                    TextEntity = _entities.Create(_currentParagraph.Identifier, replace: true)
                        .WithParent(dialogueBox)
                        .WithComponent(textVisual)
                        .WithComponent(new SmoothTextAnimation());
                }

                if (dialogueLine.Voice != null)
                {
                    Voice(dialogueLine.Voice);
                }
            }
        }

        private void Voice(Voice voice)
        {
            if (voice.Action == VoiceAction.Play)
            {
                var sound = new SoundComponent(_content.Get<AudioStream>("voice/" + voice.FileName), AudioKind.Voice);
                _entities.Create(VoiceEntityName, replace: true).WithComponent(sound);
            }
            else
            {
                //_entities.Remove(voice.FileName);
            }
        }
    }
}
