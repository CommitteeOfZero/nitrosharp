using System;
using System.Collections.Immutable;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Dialogue;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Objects;
using NitroSharp.Media;
using NitroSharp.Media.Decoding;
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
            public enum Status
            {
                LineNotLoaded,
                PlayingRevealAnimation,
                PlayingSkipAnimation,
                Waiting
            }

            public DialogueBlockSymbol DialogueBlock;
            public DialogueLine DialogueLine;
            public int CurrentDialoguePart;
            public FontFamily FontFamily;
            public Entity TextEntity;
            public TextLayout TextLayout;
            public bool StartFromNewLine;
            public bool Clear;
            public Entity PageIndicator;

            public TextRevealAnimation RevealAnimation;
            public RevealSkipAnimation RevealSkipAnimation;
            internal Voice Voice;

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

            public Status GetStatus()
            {
                if (DialogueLine == null)
                {
                    return Status.LineNotLoaded;
                }

                if ((RevealAnimation = TextEntity.GetComponent<TextRevealAnimation>()) != null)
                {
                    RevealSkipAnimation = null;
                    return Status.PlayingRevealAnimation;
                }
                if ((RevealSkipAnimation = TextEntity.GetComponent<RevealSkipAnimation>()) != null)
                {
                    RevealAnimation = null;
                    return Status.PlayingSkipAnimation;
                }

                return Status.Waiting;
            }
        }

        private const string VoiceEnityName = "__VOICE";

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
            if (state.Clear)
            {
                state.TextLayout.Clear();
                state.Clear = false;
            }
            state.Reset();
            state.DialogueLine = DialogueLine.Parse(pxmlString);
            if (state.DialogueLine.IsEmpty)
            {
                return;
            }

            double iconX = Interpreter.Globals.Get("SYSTEM_position_x_text_icon").DoubleValue;
            double iconY = Interpreter.Globals.Get("SYSTEM_position_y_text_icon").DoubleValue;
            Entity pageIndicator = state.PageIndicator;
            pageIndicator.Transform.Position = new Vector3((float)iconX, (float)iconY, 0);
            pageIndicator.Visual.IsEnabled = true;

            AdvanceDialogue();
        }

        public void Advance()
        {
            if (IsAnimationInProgress)
            {
                return;
            }

            switch (_dialogueState.GetStatus())
            {
                case DialogueState.Status.LineNotLoaded:
                    ResumeMainThread();
                    break;

                case DialogueState.Status.PlayingRevealAnimation:
                    SkipTextRevealAnimation();
                    break;

                case DialogueState.Status.PlayingSkipAnimation:
                    return;

                case DialogueState.Status.Waiting:
                    if (_dialogueState.CanAdvance)
                    {
                        AdvanceDialogue();
                    }
                    else
                    {
                        ResumeMainThread();
                    }
                    break;
            }
        }

        private void AdvanceDialogue()
        {
            DialogueState state = _dialogueState;
            ImmutableArray<DialogueLinePart> parts = state.DialogueLine.Parts;

            if (state.StartFromNewLine)
            {
                state.TextLayout.StartNewLine();
                state.StartFromNewLine = false;
            }

            uint revealStart = state.TextLayout.GlyphCount;
            for (int i = state.CurrentDialoguePart; i < parts.Length; i++)
            {
                state.CurrentDialoguePart++;
                DialogueLinePart part = parts[i];
                switch (part.PartKind)
                {
                    case DialogueLinePartKind.TextPart:
                        var textPart = (TextPart)part;
                        state.TextLayout.Append(textPart.Text, display: false);
                        break;

                    case DialogueLinePartKind.VoicePart:
                        var voicePart = (Voice)part;
                        Voice(voicePart);
                        break;

                    case DialogueLinePartKind.Marker:
                        var marker = (Marker)part;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                state.StartFromNewLine = true;
                                SuspendMainThread();
                                goto exit;

                            case MarkerKind.NoLinebreaks:
                                state.StartFromNewLine = false;
                                break;
                        }
                        break;
                }
            }

            exit:
            var animation = new TextRevealAnimation(state.TextLayout, revealStart);
            state.TextEntity.AddComponent(animation);
            SuspendMainThread();
            if (!state.CanAdvance)
            {
                animation.Completed += (obj, args) => ResumeMainThread();
            }
        }

        private void Voice(Voice voice)
        {
            _dialogueState.Voice = voice;
            if (voice.Action == VoiceAction.Play)
            {
                var audio = new MediaComponent(Content.Get<MediaPlaybackSession>("voice/" + voice.FileName), AudioSourcePool);
                _entities.Create(VoiceEnityName, replace: true).WithComponent(audio);
            }
            else
            {
                _entities.Remove(voice.FileName);
            }
        }

        private void SkipTextRevealAnimation()
        {
            var state = _dialogueState;
            var animation = state.RevealAnimation;
            var textEntity = state.TextEntity;
            if (!animation.IsAllTextVisible)
            {
                animation.Stop();
                textEntity.RemoveComponent(animation);
                if (!animation.IsAllTextVisible)
                {
                    var skip = new RevealSkipAnimation(state.TextLayout, animation.Position + 1);
                    textEntity.AddComponent(skip);
                    if (!_dialogueState.CanAdvance)
                    {
                        ResumeMainThread();
                    }
                }
            }
        }
    }
}
