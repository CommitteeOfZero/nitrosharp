using System.Collections.Immutable;
using NitroSharp.Content;
using NitroSharp.Input;
using NitroSharp.Media.Decoding;
using NitroSharp.NsScript.Execution;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp.Dialogue
{
    internal struct DialogueSystemInput
    {
        public bool AcceptUserInput;
        public DialogueSystemCommand Command;
        public Entity TextEntity;
        public DialogueLine DialogueLine;
    }

    internal enum DialogueSystemCommand
    {
        HandleInput,
        BeginDialogue
    }

    internal sealed class DialogueSystem
    {
        private readonly World _world;
        private readonly InputTracker _inputTracker;
        private readonly NsScriptInterpreter _scriptInterpreter;

        private readonly EntityTable.RefTypeRow<TextLayout> _textLayouts;
        private readonly ContentManager _content;

        private TextRevealAnimation _revealAnimation;
        private RevealSkipAnimation _revealSkipAnimation;
        private int _currentDialoguePart;
        private bool _startFromNewLine;
        private string _lastVoiceName;

        public DialogueSystem(
            World world, InputTracker inputTracker,
            NsScriptInterpreter scriptInterpreter, ContentManager content)
        {
            _world = world;
            _inputTracker = inputTracker;
            _scriptInterpreter = scriptInterpreter;
            _textLayouts = world.TextInstances.Layouts;
            _content = content;
        }

        private void ResetState()
        {
            _revealAnimation = null;
            _revealSkipAnimation = null;
            _currentDialoguePart = 0;
            _startFromNewLine = false;
        }

        public bool AdvanceDialogueState(ref DialogueSystemInput input)
        {
            if (input.Command == DialogueSystemCommand.BeginDialogue)
            {
                ResetState();
                TextLayout textLayout = _textLayouts.GetValue(input.TextEntity);
                textLayout.Clear();
                _world.TextInstances.ClearFlags.Set(input.TextEntity, true);
                AdvanceDialogue(ref input, textLayout);
                return true;
            }

            if (input.AcceptUserInput &&
                input.Command == DialogueSystemCommand.HandleInput
                && GotUserInput())
            {
                if (!input.TextEntity.IsValid)
                {
                    _scriptInterpreter.ResumeMainThread();
                    return true;
                }

                TextLayout textLayout = _textLayouts.GetValue(input.TextEntity);
                switch (GetStatus(ref input))
                {
                    case Status.LineNotLoaded:
                        _scriptInterpreter.ResumeMainThread();
                        break;

                    case Status.PlayingRevealAnimation:
                        SkipTextRevealAnimation(input.TextEntity);
                        break;

                    case Status.PlayingSkipAnimation:
                        break;

                    case Status.Waiting:
                        if (_currentDialoguePart < input.DialogueLine.Parts.Length)
                        {
                            AdvanceDialogue(ref input, textLayout);
                        }
                        else
                        {
                            _scriptInterpreter.ResumeMainThread();
                        }
                        break;
                }
            }

            return false;
        }

        private bool GotUserInput()
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

        private Status GetStatus(ref DialogueSystemInput input)
        {
            if (input.DialogueLine == null)
            {
                return Status.LineNotLoaded;
            }

            if (_world.TryGetAnimation<TextRevealAnimation>(input.TextEntity, out _revealAnimation))
            {
                _revealSkipAnimation = null;
                return Status.PlayingRevealAnimation;
            }
            if (_world.TryGetAnimation<RevealSkipAnimation>(input.TextEntity, out _revealSkipAnimation))
            {
                _revealAnimation = null;
                return Status.PlayingSkipAnimation;
            }

            return Status.Waiting;
        }

        private void AdvanceDialogue(ref DialogueSystemInput input, TextLayout textLayout)
        {
            ImmutableArray<DialogueLinePart> parts = input.DialogueLine.Parts;

            if (_startFromNewLine)
            {
                textLayout.StartNewLine();
                _startFromNewLine = false;
            }

            uint revealStart = textLayout.Glyphs.Count;
            for (int i = _currentDialoguePart; i < parts.Length; i++)
            {
                _currentDialoguePart++;
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
                                _startFromNewLine = true;
                                _scriptInterpreter.SuspendMainThread();
                                goto exit;

                            case MarkerKind.NoLinebreaks:
                                _startFromNewLine = false;
                                break;
                        }
                        break;
                }
            }

        exit:
            var animation = new TextRevealAnimation(_world, input.TextEntity, (ushort)revealStart);
            _world.ActivateAnimation(animation);
        }

        private void SkipTextRevealAnimation(Entity textEntity)
        {
            TextRevealAnimation animation = _revealAnimation;
            if (!animation.IsAllTextVisible)
            {
                animation.Stop();
                if (!animation.IsAllTextVisible)
                {
                    var skip = new RevealSkipAnimation(textEntity, (ushort)(animation.Position + 1));
                    _world.ActivateAnimation(skip);
                }
            }
        }

        private void Voice(Voice voice)
        {
            if (_lastVoiceName != null)
            {
                _world.RemoveEntity(_lastVoiceName);
            }

            if (voice.Action == VoiceAction.Play)
            {
                AssetId assetId = "voice/" + voice.FileName;
                _content.TryGet<MediaPlaybackSession>(assetId, out var session);
                Entity entity = _world.CreateAudioClip(voice.FileName, assetId, false);
                _world.AudioClips.Duration.Set(entity, session.Asset.AudioStream.Duration);

                _lastVoiceName = voice.FileName;
            }
            else
            {
                _world.RemoveEntity(voice.FileName);
            }
        }
    }
}
