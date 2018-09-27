using System.Collections.Immutable;
using NitroSharp.Content;
using NitroSharp.Media.Decoding;
using NitroSharp.NsScript.Execution;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Dialogue
{
    internal sealed class DialogueSystem
    {
        private readonly World _world;
        private readonly InputTracker _inputTracker;
        private readonly NsScriptInterpreter _scriptInterpreter;

        private readonly EntityTable.RefTypeRow<TextLayout> _textLayouts;
        private readonly ContentManager _content;

        public DialogueSystem(World world, InputTracker inputTracker, NsScriptInterpreter scriptInterpreter, ContentManager content)
        {
            _world = world;
            _inputTracker = inputTracker;
            _scriptInterpreter = scriptInterpreter;
            _textLayouts = world.TextInstances.Layouts;
            _content = content;
        }

        public void Update(float deltaTime)
        {
            if (_world.ActiveAnimationCount > 0) { return; }
            ref DialogueState state = ref _world._dialogueState;
            if (!state.TextEntity.IsValid) { return; }
            if (state.Command != DialogueState.CommandKind.NoOp || ShouldAdvance())
            {
                TextLayout textLayout = _textLayouts.GetValue(state.TextEntity);
                ref bool clearFlag = ref _world.TextInstances.ClearFlags.Mutate(state.TextEntity);
                if (state.Command == DialogueState.CommandKind.Begin)
                {
                    textLayout.Clear();
                    clearFlag = true;
                    AdvanceDialogue(textLayout);
                    state.Command = DialogueState.CommandKind.NoOp;
                    return;
                }

                switch (GetStatus())
                {
                    case Status.LineNotLoaded:
                        _scriptInterpreter.ResumeMainThread();
                        break;

                    case Status.PlayingRevealAnimation:
                        SkipTextRevealAnimation();
                        break;

                    case Status.PlayingSkipAnimation:
                        break;

                    case Status.Waiting:
                        if (_world._dialogueState.CanAdvance)
                        {
                            AdvanceDialogue(textLayout);
                        }
                        else
                        {
                            _scriptInterpreter.ResumeMainThread();
                        }
                        break;
                }
            }
        }

        private bool ShouldAdvance()
        {
            InputTracker it = _inputTracker;
            return it.IsMouseButtonDownThisFrame(MouseButton.Left) || it.IsKeyDownThisFrame(Key.Space)
                || it.IsKeyDownThisFrame(Key.Enter) || it.IsKeyDownThisFrame(Key.KeypadEnter);
        }

        public enum Status
        {
            LineNotLoaded,
            PlayingRevealAnimation,
            PlayingSkipAnimation,
            Waiting
        }

        public Status GetStatus()
        {
            ref DialogueState state = ref _world._dialogueState;
            if (state.DialogueLine == null)
            {
                return Status.LineNotLoaded;
            }

            if (_world.TryGetBehavior<TextRevealAnimation>(state.TextEntity, out state.RevealAnimation))
            {
                state.RevealSkipAnimation = null;
                return Status.PlayingRevealAnimation;
            }
            if (_world.TryGetBehavior<RevealSkipAnimation>(state.TextEntity, out state.RevealSkipAnimation))
            {
                state.RevealAnimation = null;
                return Status.PlayingSkipAnimation;
            }

            return Status.Waiting;
        }

        private void AdvanceDialogue(TextLayout textLayout)
        {
            ref DialogueState state = ref _world._dialogueState;
            ImmutableArray<DialogueLinePart> parts = state.DialogueLine.Parts;

            if (state.StartFromNewLine)
            {
                textLayout.StartNewLine();
                state.StartFromNewLine = false;
            }

            uint revealStart = textLayout.Glyphs.Count;
            for (int i = state.CurrentDialoguePart; i < parts.Length; i++)
            {
                state.CurrentDialoguePart++;
                DialogueLinePart part = parts[i];
                switch (part.PartKind)
                {
                    case DialogueLinePartKind.Text:
                        var textPart = (TextPart)part;
                        textLayout.Append(textPart.Text, display: false);
                        break;

                    case DialogueLinePartKind.Voice:
                        var voicePart = (Voice)part;
                        Voice(voicePart);
                        break;

                    case DialogueLinePartKind.Marker:
                        var marker = (Marker)part;
                        switch (marker.MarkerKind)
                        {
                            case MarkerKind.Halt:
                                state.StartFromNewLine = true;
                                _scriptInterpreter.SuspendMainThread();
                                goto exit;

                            case MarkerKind.NoLinebreaks:
                                state.StartFromNewLine = false;
                                break;
                        }
                        break;
                }
            }

        exit:
            var animation = new TextRevealAnimation(_world, state.TextEntity, (ushort)revealStart);
            _world.ActivateBehavior(animation);
        }

        private void SkipTextRevealAnimation()
        {
            ref DialogueState state = ref _world._dialogueState;
            TextRevealAnimation animation = state.RevealAnimation;
            Entity textEntity = state.TextEntity;
            if (!animation.IsAllTextVisible)
            {
                animation.Stop();
                if (!animation.IsAllTextVisible)
                {
                    var skip = new RevealSkipAnimation(state.TextEntity, (ushort)(animation.Position + 1));
                    _world.ActivateBehavior(skip);
                }
            }
        }

        private void Voice(Voice voice)
        {
            ref DialogueState state = ref _world._dialogueState;
            if (state.LastVoiceName != null)
            {
                _world.RemoveEntity(state.LastVoiceName);
            }

            if (voice.Action == VoiceAction.Play)
            {
                AssetId assetId = "voice/" + voice.FileName;
                _content.TryGet<MediaPlaybackSession>(assetId, out var session);
                Entity entity = _world.CreateAudioClip(voice.FileName, assetId, false);
                _world.AudioClips.Duration.Set(entity, session.Asset.AudioStream.Duration);

                state.LastVoiceName = voice.FileName;
            }
            else
            {
                _world.RemoveEntity(voice.FileName);
            }
        }
    }
}
