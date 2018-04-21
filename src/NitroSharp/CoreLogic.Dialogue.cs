using System;
using System.Collections.Immutable;
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
        private sealed class DialogueState
        {
            public DialogueBlockSymbol DialogueBlock;
            public DialogueLine DialogueLine;
            public int CurrentDialoguePart;
            public FontFamily FontFamily;
            public Entity TextEntity;
            public TextLayout TextLayout;
            public bool StartFromNewLine;
            public Entity PageIndicator;

            public bool CanAdvance
            {
                get => DialogueLine != null && CurrentDialoguePart < DialogueLine.Parts.Length;
            }

            public void Reset()
            {
                DialogueLine = null;
                CurrentDialoguePart = 0;
                StartFromNewLine = false;
            }
        }

        private readonly DialogueState _dialogueState = new DialogueState();
        private FontService FontService => _game.FontService;

        private void LoadPageIndicator()
        {
            var visual = PageIndicator.Load(Content, "cg/sys/icon/page");
            visual.IsEnabled = false;

            var duration = TimeSpan.FromMilliseconds(visual.IconCount * 128);
            var animation = new UIntAnimation<PageIndicator>(
                visual, (i, v) => i.ActiveIconIndex = v, 0, visual.IconCount - 1,
                duration, TimingFunction.Linear, repeat: true);

            var pageIndicator = _entities.Create("__PAGE_INDICATOR")
                .WithComponent(visual)
                .WithComponent(animation);

            RequestCore(pageIndicator, NsEntityAction.Lock);
            _dialogueState.PageIndicator = pageIndicator;
        }

        public override void CreateDialogueBox(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y, int width, int height)
        {
            // TODO: remove the hardcoded text color.
            var boxRect = new RectangleVisual(width, height, RgbaFloat.White, 0.0f, 0);
            boxRect.IsEnabled = false;

            var dialogueBox = _entities.Create(entityName, replace: true)
                .WithComponent(boxRect)
                .WithPosition(x, y);

            // TODO: move this line to SetFont.
            _dialogueState.FontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
        }

        protected override void OnDialogueBlockEntered(DialogueBlockSymbol dialogueBlock)
        {
            var state = _dialogueState;
            state.DialogueBlock = dialogueBlock;
            if (_entities.TryGet(dialogueBlock.AssociatedBox, out var dialogueBox))
            {
                state.TextEntity?.Destroy();

                var boxSize = dialogueBox.Visual.Bounds;
                const float TextRightMargin = 200;
                var layoutBounds = new Size((uint)boxSize.Width - (uint)TextRightMargin, (uint)boxSize.Height);
                var textLayout = new TextLayout(256, state.FontFamily, layoutBounds);
                var textEntity = _entities.Create(state.DialogueBlock.Identifier)
                    .WithParent(dialogueBox)
                    .WithComponent(textLayout);

                textEntity.Transform.Position.Y += 10;
                state.TextEntity = textEntity;
                state.TextLayout = textLayout;
            }
        }

        public override void BeginDialogue(string pxmlString)
        {
            var state = _dialogueState;
            state.Reset();
            state.TextLayout.Clear();
            state.DialogueLine = DialogueLine.Parse(pxmlString);

            double iconX = Interpreter.Globals.Get("SYSTEM_position_x_text_icon").DoubleValue;
            double iconY = Interpreter.Globals.Get("SYSTEM_position_y_text_icon").DoubleValue;
            Entity pageIndicator = state.PageIndicator;
            pageIndicator.Transform.Position = new Vector3((float)iconX, (float)iconY, 0);
            pageIndicator.Visual.IsEnabled = true;

            AdvanceDialogue();
        }

        public void Advance()
        {
            var textEntity = _dialogueState.TextEntity;
            if (textEntity != null)
            {
                var reveal = textEntity.GetComponent<TextRevealAnimation>();
                if (reveal != null)
                {
                    SkipTextRevealAnimation(reveal);
                    return;
                }

                var skipAnimation = textEntity.GetComponent<RevealSkipAnimation>();
                if (skipAnimation != null)
                {
                    return;
                }
            }

            bool waitingForInput = MainThread.SleepTimeout == TimeSpan.MaxValue;
            if (waitingForInput && !_dialogueState.CanAdvance)
            {
                Interpreter.ResumeThread(MainThread);
            }
            else if (waitingForInput)
            {
                AdvanceDialogue();
            }
        }

        private void AdvanceDialogue()
        {
            DialogueState state = _dialogueState;
            ImmutableArray<DialogueLinePart> parts = state.DialogueLine.Parts;
            for (int i = state.CurrentDialoguePart; i < parts.Length; i++)
            {
                state.CurrentDialoguePart++;
                DialogueLinePart part = parts[i];
                switch (part.PartKind)
                {
                    case DialogueLinePartKind.TextPart:
                        var textPart = (TextPart)part;
                        uint positionInText = state.TextLayout.GlyphCount;
                        if (state.StartFromNewLine)
                        {
                            state.TextLayout.StartNewLine();
                            state.StartFromNewLine = false;
                        }

                        state.TextLayout.Append(textPart.Text, display: false);
                        var animation = new TextRevealAnimation(state.TextLayout, positionInText);
                        state.TextEntity.AddComponent(animation);
                        break;

                    case DialogueLinePartKind.VoicePart:
                        var voicePart = (Voice)part;
                        // TODO: play voice
                        break;

                    case DialogueLinePartKind.Marker:
                        var marker = (Marker)part;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                state.StartFromNewLine = true;
                                return;

                            case MarkerKind.NoLinebreaks:
                                state.StartFromNewLine = false;
                                break;
                        }
                        break;
                }
            }
        }

        private void SkipTextRevealAnimation(TextRevealAnimation animation)
        {
            var state = _dialogueState;
            var textEntity = state.TextEntity;
            if (!animation.IsAllTextVisible)
            {
                animation.Stop();
                textEntity.RemoveComponent(animation);
                if (!animation.IsAllTextVisible)
                {
                    var skip = new RevealSkipAnimation(state.TextLayout, animation.Position + 1);
                    textEntity.AddComponent(skip);
                }
            }
        }
    }
}
